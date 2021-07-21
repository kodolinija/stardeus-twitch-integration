using System.Collections;
using Game.Data;
using Game.Data.Twitch;
using Game.Systems;
using KL.Commands;
using KL.I18N;
using KL.Integrations.Twitch;
using KL.Randomness;
using UnityEngine;

namespace Game.Commands.Twitch {
    public class CmdTwitchExplode : AsyncCommand {
        public override bool IsUnique => false;
        private const string cmd = "!explode";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType
            .SubsystemRegistration)]
        protected static void Register() {
            TwitchSys.RegisterCommand(cmd, (msg, viewer) => 
                new CmdTwitchExplode(viewer, msg), 
                "twitch.cmd.explode.help");
        }

        public override string Id => "twitch_!explode";
        private TwitchViewer viewer;
        private TwitchMessage msg;

        public CmdTwitchExplode(TwitchViewer viewer, TwitchMessage msg) {
            this.viewer = viewer;
            this.msg = msg;
        }

        public override IEnumerator ExecuteAsync() {
            if (!viewer.HasRole("play")) { yield break; }
            if (!viewer.HasRole("destroy")) { yield break; }
            if (viewer.Being.TwitchViewer.IsExplosionPending) { yield break; }
            if (viewer.Reputation < 1000) {
                A.Sys.Twitch.Client.ReplyTo(msg, "twitch.cmd.explode.rep"
                    .T(msg.Username, 1000, viewer.Reputation));
                yield break;
            }
            viewer.AddReputation(-1000);
            yield return viewer.Being.TwitchViewer.CountdownToExplosion();
        }
    }
}