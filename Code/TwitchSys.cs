//#define DEBUG_TWITCH
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Game.Commands;
using Game.Commands.Twitch;
using Game.Constants;
using Game.Data;
using Game.Data.Twitch;
using Game.UI;
using Game.Utils;
using KL.Clock;
using KL.Collections;
using KL.Commands;
using KL.I18N;
using KL.Integrations.Twitch;
using KL.Utils;
using UnityEngine;

namespace Game.Systems {
    public sealed class TwitchSys : ISystem {
        public const string Id = "TwitchSys";
        private const int RepeatAnnounceAfterMessages = 20;
        // TODO increase announce delay when implemented
        private const int RepeatAnnounceAfterMillis = 60 * 60 * 1000;
        public readonly Dictionary<string, TwitchRole> Roles 
            = new Dictionary<string, TwitchRole>();
        private static readonly Dictionary<string, Func<TwitchMessage, TwitchViewer, ICommand>> KnownCommands 
            = new Dictionary<string, Func<TwitchMessage, TwitchViewer, ICommand>>();

        private static readonly Dictionary<string, string> HelpLines
            = new Dictionary<string, string>();

        private int channelMessagesSinceLastAnnounce;
        private int millisSinceLastAnnounce;
        private bool isInitialized;

        public bool IsInitialized => isInitialized;

        public void PostReputation(TwitchMessage msg) {
            var name = msg.Args;
            if (string.IsNullOrWhiteSpace(name)) {
                name = msg.Username;
            }
            var viewer = FindViewer(name);
            if (viewer == null) { return; }
            twitch.ReplyTo(msg, string.Format("{0} {1}", 
                viewer.Name, "twitch.reputation".T(viewer.Reputation)));
        }


        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType
            .SubsystemRegistration)]
        static void Register() {
            GameSystems.Register(Id, () => new TwitchSys());
            The.Prefs.RegisterString(Pref.GTwitchBotName, null);
            The.Prefs.RegisterString(Pref.GTwitchBotOAuth, null);
            The.Prefs.RegisterString(Pref.GTwitchChannel, null);
            The.Prefs.RegisterBool(Pref.GTwitchIsActive, false);
        }

        private GameState state;
        private static Twitch twitch;
        public Twitch Client => twitch;
        private List<TwitchViewer> viewers;
        private bool isActive;
        public int PlayerCount {
            get {
                var count = 0;
                for (int i = 0; i < viewers.Count; i++) {
                    if (viewers[i].HasRole("play")) {
                        count++;
                    }
                }
                return count;
            }
        }

        public void Initialize() {
            A.Sig.AfterLoadState.AddListener(OnAfterLoad);
        }

        private static Twitch testTwitch;

        public static void TestConfiguration() {
            try {
                if (testTwitch != null) {
                    UIPopupWidget.Spawn(IconId.CWarning, 
                        "twitch.error".T(),
                        "twitch.error.previous.test.running".T());
                    return;
                }
                CleanupTest();
                var conn = CreateConnection();
                testTwitch = new Twitch(conn);
                testTwitch.ReconnectIfNeeded();
                Coroutines.Start(SendTestMessage(), 
                    "TwitchSystem", "SendTestMessage");
            } catch (Exception ex) {
                UIPopupWidget.Spawn(IconId.CWarning, 
                    "twitch.error".T(),
                    ex.Message);
            }
        }

        public static void CleanupTest() {
            if (testTwitch != null) {
                testTwitch.Destroy();
                testTwitch = null;
            }
        }

        private static IEnumerator SendTestMessage() {
            var startWait = Time.realtimeSinceStartup;
            while (!testTwitch.IsConnected) {
                if (Time.realtimeSinceStartup - startWait > 10) {
                    D.Err("Could not connect to Twitch in 10 seconds");
                    UIPopupWidget.Spawn(IconId.CWarning, 
                        "twitch.error".T(),
                        "twitch.error.could.not.connect".T());
                    break;
                }
                D.Log("State: {0}", testTwitch.State);
                yield return null;
            }
            if (testTwitch.IsConnected) {
                try {
                    testTwitch.SendChannelMessage("twitch.test.message".T());
                    D.Warn("Sending a test message to twitch");
                } catch (Exception ex) {
                    UIPopupWidget.Spawn(IconId.CWarning, 
                        "twitch.error".T(),
                        ex.Message);
                }
            }
            yield return Consts.Wait100Ms;
            CleanupTest();
        }

        private static TwitchConnection CreateConnection() {
            var isActive = The.Prefs.GetBool(
                Pref.GTwitchIsActive, false);

            // Create this so we'll have settings 
            var cfg = new TwitchConnection {
                IsActive = isActive,
                ChannelName = The.Prefs.GetString(
                    Pref.GTwitchChannel, null),
                BotName = The.Prefs.GetString(
                    Pref.GTwitchBotName, null),
                OAuth = The.Prefs.GetString(
                    Pref.GTwitchBotOAuth, null),
            };

            if (!isActive) { 
                #if DEBUG_TWITCH
                D.Log("Twitch is inactive!");
                #endif
            }
            return cfg;
        }

        private void SetupTwitch() {
            if (isInitialized) { return; }
            var conn = CreateConnection();
            isActive = conn.IsActive;
            if (!isActive) { return; }
            if (twitch == null) {
                twitch = new Twitch(conn);
            }

            if (twitch.IsConfigured) {
                #if DEBUG_TWITCH
                D.Log("Twitch integration is live!");
                #endif
                viewers = TwitchViewerMemory.Load();
                A.State.Clock.OnTick.AddListener(OnTick);
                twitch.OnCommand.AddListener(OnCommand);
                twitch.OnMessage.AddListener(OnMessage);
                twitch.OnPart.AddListener(OnPart);
                twitch.OnJoin.AddListener(OnJoin);
                Coroutines.Start(Configure(), this, "TwitchInitialSetup");
            } else {
                #if DEBUG_TWITCH
                D.Warn("Twitch is not configured!");
                #endif
            }
        }

        private IEnumerator RejoinViewers() {
            yield return Ready.WaitForInput(this);
            // Restore the play state of all viewers
            #if DEBUG_TWITCH
            D.Log("Twitch: rejoining viewers");
            #endif
            foreach (var viewer in viewers) {
                if (!viewer.HasRole("play")) { continue; }
                #if DEBUG_TWITCH
                D.Log("Twitch: rejoining viewer: {0}", viewer.Name);
                #endif
                viewer.RemoveRole("play");
                viewer.Being = null;
                viewer.AddCommand(state.Clock.Ticks, 
                    new CmdTwitchSpawnViewer(viewer, null));
            }
        }

        private void OnAfterLoad(GameState state) {
            this.state = state;
            SetupTwitch();
            // We didn't run setup asyncronously
            if (isInitialized) {
                Coroutines.Start(RejoinViewers(), this, "RejoinViewers");
            }
        }

        public void ViewerJoined(TwitchViewer viewer) {
            OnViewerChanged(viewer);
        }
        public void ViewerLeft(TwitchViewer viewer) {
            OnViewerChanged(viewer);
        }

        private void OnViewerChanged(TwitchViewer viewer) {
            A.CmdQ.Enqueue(new CmdExecuteAction(SignalViewerCountChange));
        }

        private void OnJoin(string nick) {
            var viewer = FindViewer(nick);
            if (viewer != null) {
                twitch.SendChannelMessage("twitch.viewer.returned".T(nick));
            } else {
                var ticksNow = Environment.TickCount;
                if (ticksNow - millisSinceLastAnnounce > RepeatAnnounceAfterMillis) {
                    SendWelcomeMessage();
                }
            }
        }

        private void OnPart(string nick) {
            var viewer = FindViewer(nick);
            if (viewer != null && viewer.HasRole("play")) {
                twitch.SendChannelMessage("twitch.viewer.left".T(nick));
                viewer.AddCommand(A.Ticks, 
                    new CmdTwitchLeave(viewer));
            }
        }

        private void SignalViewerCountChange() {
            The.SysSig.TwitchViewerCountChanged.Send(PlayerCount);
        }

        private IEnumerator Configure() {
            var state = A.State;
            var roles = The.ModLoader.LoadObjects<TwitchRole>(
                "Config/Twitch/Roles");
            foreach (var role in roles) {
                Roles.Put(role.Role, role);
            }
            yield return Ready.WaitForInput(this);
            if (state.IsGenFailure) { yield break; }
            twitch.ReconnectIfNeeded();
            The.SysSig.TwitchActive.Send(true);
            #if !UNITY_EDITOR
            SendWelcomeMessage();
            #else
            channelMessagesSinceLastAnnounce = 0;
            millisSinceLastAnnounce = Environment.TickCount;
            #endif
            yield return RejoinViewers();
            isInitialized = true;
        }

        private void SendWelcomeMessage() {
            #if UNITY_EDITOR
            return;
            #else
            A.CmdQ.Enqueue(new CmdExecuteAction(() => {
                channelMessagesSinceLastAnnounce = 0;
                millisSinceLastAnnounce = Environment.TickCount;
                twitch.SendChannelMessage(string.Format("/me {0}", 
                    "twitch.welcome".T()));
            }));
            #endif
        }

        public void OnTick(long ticks) {
            if (ticks % 51 == 0) {
                twitch.ReconnectIfNeeded();
                UpdateReputation();
            }
        }

        private void UpdateReputation() {
            for (int i = 0; i < viewers.Count; i++) {
                var viewer = viewers[i];
                if (viewer.HasRole("play")) {
                    ApplyRoleChanges(viewer, viewer.UpdateReputation());
                }
            }
        }

        public void ApplyRoleChanges(TwitchViewer viewer, float lastRep) {
            foreach (var role in Roles) {
                viewer.ApplyRoleChange(role.Value, lastRep);
            }
        }
        // FIXME if first message comes in form of whisper, we don't get 
        // anything about the user!
        public void SetInitialRoles(TwitchViewer viewer, TwitchMessage msg) {
            if (msg.ParsePrivileges(out var b, out var v, out var m)) {
                viewer.IsBroadcaster = b;
                viewer.IsVIP = v;
                viewer.IsMod = m;
            }
            foreach (var role in Roles) {
                var r = role.Value;
                if (r.AddToBroadcaster && viewer.IsBroadcaster
                        || r.AddToMods && viewer.IsMod
                        || r.AddToVIP && viewer.IsVIP) {
                    viewer.AddRole(r.Role);
                }
            }
        }

        private void OnMessage(TwitchMessage message) {
            // Have to enqueue commands here, otherwise it would not run
            // on main thread!
            try {
                if (!The.GameIsInitialized) { return; }
                if (message.IsChannelMessage) {
                    if (channelMessagesSinceLastAnnounce++ > 
                            RepeatAnnounceAfterMessages) {
                        var ticksNow = Environment.TickCount;
                        if (ticksNow - millisSinceLastAnnounce > 
                                RepeatAnnounceAfterMillis) {
                            SendWelcomeMessage();
                        }
                    }
                }
            } catch (System.Exception ex) {
                D.LogEx(ex, "Failed processing Twitch command !join");
            }
        }

        private void OnCommand(TwitchMessage message) {
            // Have to enqueue commands here, otherwise it would not run
            // on main thread!
            try {
                if (!The.GameIsInitialized) { return; }
                var viewer = FindOrCreateViewer(message);
                var tick = A.Ticks;
                viewer.AddCommand(tick, ParseCommand(viewer, message));
            } catch (System.Exception ex) {
                D.LogEx(ex, "Failed processing Twitch command !join");
            }
        }

        private ICommand ParseCommand(TwitchViewer viewer, TwitchMessage message) {
            var cmd = message.Command;
            if (KnownCommands.TryGetValue(cmd, out var act)) {
                return act(message, viewer);
            }
            return null;
        }

        public TwitchViewer FindViewer(string name) {
            foreach (var v in viewers) {
                if (v.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) { 
                    return v;
                }
            }
            return null;
        }

        private TwitchViewer FindOrCreateViewer(TwitchMessage msg) {
            var v = FindViewer(msg.Username);
            if (v != null) { return v; }
            var viewer = new TwitchViewer();
            viewer.Name = msg.Username;
            SetInitialRoles(viewer, msg);
            viewers.Add(viewer);
            OnViewerChanged(viewer);
            // Cannot send this here, it may come from non main thread
            //The.SysSig.TwitchViewerCountChanged.Send(PlayerCount);
            return viewer;
        }

        public void Unload() {
            isInitialized = false;
            if (twitch != null) {
                twitch.Destroy();
                twitch = null;
            }
            if (!Arrays.IsEmpty(viewers)) {
                TwitchViewerMemory.Save(viewers);
            }
        }

        public static void RegisterCommand(string cmd, 
                Func<TwitchMessage, TwitchViewer, ICommand> act, 
                string helpLine) {
            #if DEBUG_TWITCH
            D.Log("Adding Twitch command: {0}", cmd);
            #endif
            KnownCommands.Put(cmd, act);
            if (!string.IsNullOrWhiteSpace(helpLine)) { 
                HelpLines.Put(cmd, helpLine);
            }
        }

        public void ShowHelp(TwitchMessage msg) {
            channelMessagesSinceLastAnnounce = 0;
            var args = msg.Args;
            if (string.IsNullOrWhiteSpace(args)) {
                twitch.ReplyTo(msg, string.Format("/me {0}: {1} • {2}", 
                    "twitch.help.commands".T(), 
                    string.Join(" • ", HelpLines.Keys), 
                    "twitch.help.more".T()));
                return;
            }
            var line = HelpLines.Get(args, null);
            if (line == null) {
                line = HelpLines.Get($"!{args}", "twitch.cmd.missing");
            }
            twitch.ReplyTo(msg, $"/me {line.T()}");
        }
    }
}