using Game.Data;
using Game.Systems;
using KL.Commands;
using KL.I18N;
using KL.Integrations.Twitch;
using UnityEngine;

namespace Game.Commands.Twitch {
    public class CmdTwitchMe : Command {
        public override bool IsUnique => false;
        private const string cmd = "!me";
        private TwitchMessage msg;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType
            .SubsystemRegistration)]
        protected static void Register() {
            TwitchSys.RegisterCommand(cmd, 
                (args, viewer) => new CmdTwitchMe(args),
                "twitch.cmd.me.help");
        }
        public override string Id => "twitch_!me";

        private CmdTwitchMe(TwitchMessage msg) {
            this.msg = msg;
        }

        public override void Execute() {
            var name = msg.Args;
            if (name == null) { name  = msg.Username; }
            var yourself = name == msg.Username;
            var viewer = A.Sys.Twitch.FindViewer(name);
            var twitch = A.Sys.Twitch.Client;
            if (viewer == null || !viewer.HasRole("play")) {
                if (yourself) {
                    twitch.ReplyTo(msg, "twitch.not.playing.me".T(name)); 
                } else {
                    twitch.ReplyTo(msg, "twitch.not.playing.other".T(name));
                }
            }
            if (yourself) {
                twitch.ReplyTo(msg, $"/me @{name}: {viewer.Info}");
            } else {
                twitch.ReplyTo(msg, $"/me {name}: {viewer.Info}");
            }
        }
    }
}