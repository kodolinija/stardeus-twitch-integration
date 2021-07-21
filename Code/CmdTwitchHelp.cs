using Game.Data;
using Game.Systems;
using KL.Commands;
using KL.I18N;
using KL.Integrations.Twitch;
using UnityEngine;

namespace Game.Commands.Twitch {
    public class CmdTwitchHelp : Command {
        public override bool IsUnique => false;
        private const string cmd = "!help";
        private TwitchMessage msg;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType
            .SubsystemRegistration)]
        protected static void Register() {
            TwitchSys.RegisterCommand(cmd, 
                (args, viewer) => new CmdTwitchHelp(args),
                null);
        }
        public override string Id => "twitch_!help";

        private CmdTwitchHelp(TwitchMessage msg) {
            this.msg = msg;
        }

        public override void Execute() {
            A.Sys.Twitch.ShowHelp(msg);
        }
    }
}