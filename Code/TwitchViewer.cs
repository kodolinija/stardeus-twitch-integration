//#define DEBUG_TWITCH
using System;
using System.Collections.Generic;
using System.Text;
using Game.Components;
using Game.Constants;
using KL.Collections;
using KL.Commands;
using KL.I18N;
using KL.Utils;
using UnityEngine;

namespace Game.Data.Twitch {
    [Serializable]
    public class TwitchViewer {
        private const float ReputationPerMillisecond = 0.1f / 1000f;
        private const int MaxCommandsPerTimeLimit = 10;
        private const long TimeLimitTicks = 100;
        private HashSet<string> roles = new HashSet<string>();
        private int lastEnvTick;
        public float Reputation;
        public string Name;
        private Being being;
        public bool IsBroadcaster;
        public bool IsVIP;
        public bool IsMod;
        public string Info {
            get {
                var data = new List<string>();
                data.Add("twitch.reputation".T(Reputation));
                var privs = Privileges;
                data.AddNotNull(privs);
                if (Being != null) {
                    data.AddNotNull(Being.Transform.Info);
                    data.AddNotNull(Being.Navigation.Info);
                    data.AddNotNull(Being.Brain.Info);
                    data.AddNotNull(Being.Labor.Info);
                    data.AddNotNull(Being.Health.Info);
                    var nrgNeed = Being.Needs.EnergyNeed;
                    data.Add(string.Format("{0}: {1:0}/{2:0}", 
                        nrgNeed.NeedName, nrgNeed.Value, nrgNeed.Max));
                }
                return string.Join(", ", data);
            }
        }
        public Being Being { 
            get => being;
            set {
                being = value;
                if (being == null) {
                    viewerComp = null;
                    lastEnvTick = 0;
                }
            }
        }
        private TwitchViewerComp viewerComp;
        private long lastCommandTick;
        public readonly List<ICommand> LastCommands = new List<ICommand>();

        public string Privileges {
            get {
                if (IsBroadcaster) {
                    return "twitch.priv.broadcaster".T();
                }
                if (IsMod) {
                    return "twitch.priv.mod".T();
                }
                if (IsVIP) {
                    return "twitch.priv.vip".T();
                }
                return null;
            }
        }

        public float UpdateReputation() {
            // FIXME remove TickCount
            var tickNow = Environment.TickCount;
            var diff = tickNow - lastEnvTick;
            var last = lastEnvTick;
            lastEnvTick = tickNow;
            if (last == 0) { return 0; }
            return AddReputation(diff * ReputationPerMillisecond);
        }

        public float AddReputation(float amount) {
            /*
            TODO: We should listen to OnAdvertCompleted signal and then do this:
            Viewer.AddReputation(10);
            */
            var lastRep = Reputation;
            Reputation += amount;
            if (Being != null) {
                if (viewerComp == null) {
                    viewerComp = Being.GetComponent<TwitchViewerComp>();
                }
            }
            viewerComp?.UpdateUIBlock(true);
            return lastRep;
        }

        public void ApplyRoleChange(TwitchRole role, float lastRep) {
            // Don't check for stuff we already added
            if (IsBroadcaster && role.AddToBroadcaster) { return; }
            if (IsMod && role.AddToMods) { return; }
            var addAtRep = role.AddAtReputation;
            var removeAtRep = role.RemoveAtReputation;
            if (IsVIP) {
                if (role.AddToVIP) { return; }
                if (role.VIPAddAtReputation > 0) {
                    addAtRep = role.VIPAddAtReputation;
                }
                if (role.VIPRemoveAtReputation > 0) {
                    removeAtRep = role.VIPRemoveAtReputation;
                }
            }
            // Not auto added or removed
            if (addAtRep == 0 && removeAtRep == 0) { return; }
            if (removeAtRep > 0 && lastRep > removeAtRep && Reputation <= removeAtRep) {
                if (roles.Remove(role.Role)) {
                    viewerComp?.UpdateUIBlock(true);
                }
            } else if (addAtRep > 0 && lastRep < addAtRep && Reputation >= addAtRep) {
                if (roles.Add(role.Role)) {
                    viewerComp?.UpdateUIBlock(true);
                }
            }
        }

        public void AddRole(string role) {
            roles.Add(role);
        }

        public void RemoveRole(string role) {
            roles.Remove(role);
        }

        public bool HasRole(string role) {
            return roles.Contains(role);
        }

        public void AddCommand(long tick, ICommand command) {
            #if DEBUG_TWITCH
            D.Log("Adding cmd to {0}: {1}", Name, command);
            #endif
            if (command == null) { return; }
            #if DEBUG_TWITCH
            if (lastCommandTick == tick) {
                D.Warn("More than one command per tick for twitch viewer {0}!",
                    Name);
            }
            #endif
            lastCommandTick = tick;
            CleanupOldCommands(tick);
            if (LastCommands.Count > MaxCommandsPerTimeLimit) {
                D.Warn("Command throttled for {0} -> {1} commands in last {2} ticks!", 
                    Name, LastCommands.Count, TimeLimitTicks);
                return;
            }
            LastCommands.Add(command);
            A.CmdQ.Enqueue(command);
        }

        private void CleanupOldCommands(long tick) {
            var threshold = tick + TimeLimitTicks;
            for (int i = LastCommands.Count - 1; i > 0; i--) {
                var cmd = LastCommands[i];
                if (cmd.EnqueueTick < threshold) {
                    LastCommands.RemoveAt(i);
                }
            }
        }
    }
}