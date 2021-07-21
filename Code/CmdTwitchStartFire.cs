using Game.Data;
using Game.Data.Twitch;
using Game.Systems;
using KL.Commands;
using KL.I18N;
using UnityEngine;

namespace Game.Commands.Twitch {
    public class CmdTwitchStartFire : Command {
        public override bool IsUnique => false;
        private const string cmd = "!fire";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType
            .SubsystemRegistration)]
        protected static void Register() {
            TwitchSys.RegisterCommand(cmd, 
                (args, viewer) => new CmdTwitchStartFire(viewer),
                "twitch.cmd.fire.help");
        }

        public override string Id => "twitch_!fire";
        private TwitchViewer viewer;

        public CmdTwitchStartFire(TwitchViewer viewer) {
            this.viewer = viewer;
        }

        public override void Execute() {
            if (!viewer.HasRole("destroy")) { return; }
            if (viewer.Being == null) { return; }
            var tile = EntityUtils.GetTopmostTile(viewer.Being.PosIdx);
            if (tile == null) { return; }
            tile.Flammable?.SetOnFire();
        }
    }
}