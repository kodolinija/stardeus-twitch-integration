using System;
using Game.AI.Actions;
using Game.Constants;
using Game.Data;
using Game.Data.Twitch;
using Game.Systems;
using KL.Commands;
using KL.Grid;
using KL.Integrations.Twitch;
using UnityEngine;

namespace Game.Commands.Twitch {
    public class CmdTwitchGoTo : Command {
        public override bool IsUnique => false;
        private const string cmd = "!goto";
        private TwitchMessage msg;
        private TwitchViewer viewer;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType
            .SubsystemRegistration)]
        protected static void Register() {
            TwitchSys.RegisterCommand(cmd, 
                (msg, viewer) => new CmdTwitchGoTo(msg, viewer), 
                "twitch.cmd.goto.help");
        }
        public override string Id => "twitch_!goto";

        private CmdTwitchGoTo(TwitchMessage msg, TwitchViewer viewer) {
            this.msg = msg;
            this.viewer = viewer;
        }

        public override void Execute() {
            if (!viewer.HasRole("play")) { return; }
            int pos;
            if (msg.Args != null) { 
                var coords = msg.Args.Split(' ');
                if (coords.Length != 2) { return; }
                if (!Int32.TryParse(coords[0], out int x)) { return; }
                if (!Int32.TryParse(coords[1], out int y)) { return; }
                pos = Pos.FromXY(x, y);
            } else {
                var camPos = A.State.BB.Get(
                    BBKey.CameraPosition, 
                    Vector2.zero);
                if (camPos == Vector2.zero) { return; }
                pos = EntityUtils.ToPos(camPos);
            }
            if (!A.State.Grid.IsWithinBounds(pos)) { return; }
            if (viewer.Being.Labor.IsBusy) {
                if (viewer.Being.Brain.CurrentAd != null) {
                    viewer.AddReputation(-10);
                }
                var exec = new ExecutionResult {
                    IsFinished = true,
                    IsSuccess = false,
                    FailReason = "Twitch command",
                };
                viewer.Being.Labor.CurrentAct.Cancel(exec);
            }
            viewer.Being.Labor.SetTask(ActMoveToPos
                .SubActionToOrNextTo(pos, viewer.Being, true));
        }
    }
}