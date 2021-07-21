using Game.Data;
using Game.Data.Twitch;
using Game.Systems;
using KL.Commands;
using KL.Integrations.Twitch;
using UnityEngine;

namespace Game.Commands.Twitch {
    public class CmdTwitchRep : Command {
        public override bool IsUnique => false;
        private const string cmd = "!rep";
        private TwitchMessage msg;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType
            .SubsystemRegistration)]
        protected static void Register() {
            TwitchSys.RegisterCommand(cmd, 
                (msg, viewer) => new CmdTwitchRep(msg, viewer), 
                "twitch.cmd.rep.help");
        }
        public override string Id => "twitch_!rep";

        private CmdTwitchRep(TwitchMessage msg, TwitchViewer viewer) {
            this.msg = msg;
        }

        public override void Execute() {
            A.Sys.Twitch.PostReputation(msg);
        }
    }
}