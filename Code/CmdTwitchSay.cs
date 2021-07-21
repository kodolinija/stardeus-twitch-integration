using System.Collections;
using Game.Data;
using Game.Data.Twitch;
using Game.Systems;
using KL.Commands;
using KL.I18N;
using KL.Text;
using UnityEngine;

namespace Game.Commands.Twitch {
    public class CmdTwitchSay : AsyncCommand {
        public override bool IsUnique => false;
        public static readonly Vector2 TextOffset = new Vector2(0, 1f);
        private const string cmd = "!say";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType
            .SubsystemRegistration)]
        protected static void Register() {
            TwitchSys.RegisterCommand(cmd,
                (msg, viewer) => new CmdTwitchSay(viewer, msg.Args),
                "twitch.cmd.say.help");
        }
        private const float MessageDuration = 5;
        public override string Id => "twitch_!say";
        private TwitchViewer viewer;
        private string message;

        public CmdTwitchSay(TwitchViewer viewer, string message) {
            this.viewer = viewer;
            this.message = message;
        }

        public override IEnumerator ExecuteAsync() {
            if (!viewer.HasRole("play")) { yield break; }
            if (!viewer.HasRole("talk")) { yield break; }
            if (string.IsNullOrWhiteSpace(message)) { yield break; }
            var text = GameText.Create("OverheadMessage", 
                $"{viewer.Name}: {message}", 
                viewer.Being.Graphics.Position + TextOffset);
            var start = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - start < MessageDuration) {
                MoveMessage(text);
                yield return null;
            }
            text.Destroy();
        }

        private void MoveMessage(GameText text) {
            text.MoveTo(viewer.Being.Graphics.Position + TextOffset);
        }
    }
}