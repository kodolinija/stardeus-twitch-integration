using Game.Data;
using Game.Systems;
using KL.Commands;
using KL.Grid;
using KL.I18N;
using KL.Integrations.Twitch;
using UnityEngine;

namespace Game.Commands.Twitch {
    public class CmdTwitchCam : Command {
        public override bool IsUnique => false;
        private const string cmd = "!cam";
        private TwitchMessage msg;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType
            .SubsystemRegistration)]
        protected static void Register() {
            TwitchSys.RegisterCommand(cmd, 
                (args, viewer) => new CmdTwitchCam(args),
                "twitch.cmd.cam.help");
        }
        public override string Id => "twitch_!cam";

        private CmdTwitchCam(TwitchMessage msg) {
            this.msg = msg;
        }

        public override void Execute() {
            if (!Ready.Game) { return; }
            var camPos = A.State.BB.Get(
                BBKey.CameraPosition, Vector2.zero);
            var pos = EntityUtils.ToPos(camPos);
            A.Sys.Twitch.Client.ReplyTo(msg, "twitch.cam".T(
                Pos.String(pos)));
        }
    }
}