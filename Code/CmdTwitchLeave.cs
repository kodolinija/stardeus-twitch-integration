using Game.Data;
using Game.Data.Twitch;
using Game.Systems;
using KL.Commands;
using KL.I18N;
using UnityEngine;

namespace Game.Commands.Twitch {
    public class CmdTwitchLeave : Command {
        public override bool IsUnique => false;
        private const string cmd = "!leave";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType
            .SubsystemRegistration)]
        protected static void Register() {
            TwitchSys.RegisterCommand(cmd,
                (args, viewer) => new CmdTwitchLeave(viewer),
                "twitch.cmd.leave.help");
        }
        public override string Id => "twitch_!leave";
        private TwitchViewer viewer;

        public CmdTwitchLeave(TwitchViewer viewer) {
            this.viewer = viewer;
        }

        public override void Execute() {
            viewer.RemoveRole("play");
            A.Sys.Twitch.ViewerLeft(viewer);
            if (viewer.Being != null) {
                A.State.Beings.Destroy(viewer.Being);
                viewer.Being = null;
            }
        }
    }
}