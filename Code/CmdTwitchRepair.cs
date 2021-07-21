using System.Collections;
using System.Threading.Tasks;
using Game.Components;
using Game.Constants;
using Game.Data;
using Game.Data.Twitch;
using Game.Systems;
using KL.Commands;
using KL.Grid;
using KL.Integrations.Twitch;
using UnityEngine;

namespace Game.Commands.Twitch {
    public class CmdTwitchRepair : AsyncCommand {
        public override bool IsUnique => false;
        private const string cmd = "!repair";
        private TwitchMessage msg;
        private TwitchViewer viewer;
        private int posIdx;
        private DamageableComp dmg;


        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType
            .SubsystemRegistration)]
        protected static void Register() {
            TwitchSys.RegisterCommand(cmd, 
                (msg, viewer) => new CmdTwitchRepair(msg, viewer), 
                "twitch.cmd.repair.help");
        }
        public override string Id => "twitch_!repair";

        private CmdTwitchRepair(TwitchMessage msg, TwitchViewer viewer) {
            this.msg = msg;
            this.viewer = viewer;
        }

        private bool Check(DamageableComp dmg) {
            return dmg.IsDamaged && dmg.CurrentAdvert != null 
                && dmg.CurrentAdvert.WorkerEntityId == 0 && 
                    Pos.SqrDistance(posIdx, dmg.Entity.PosIdx) < 400;
        }

        public override IEnumerator ExecuteAsync() {
            if (!viewer.HasRole("play")) { yield break; }
            if (!viewer.HasRole("work")) { yield break; }
            if (viewer.Being != null) {
                if (viewer.Being.Brain.CurrentAd != null) {
                    yield break;
                }
                this.posIdx = viewer.Being.PosIdx;
            } else {
                yield break;
            }

            var t = Task.Run(() => {
                dmg = A.State.Components.FindFirstMatching<DamageableComp>(
                    WorldLayer.Objects, Check);
            });
            while (!t.IsCompleted) { yield return null; }
            if (dmg == null) {
                t = Task.Run(() => {
                    dmg = A.State.Components.FindFirstMatching<DamageableComp>(
                        WorldLayer.Floor, Check);
                });
                while (!t.IsCompleted) { yield return null; }
            } 
            if (dmg == null) {
                t = Task.Run(() => {
                    dmg = A.State.Components.FindFirstMatching<DamageableComp>(
                        WorldLayer.Walls, Check);
                });
                while (!t.IsCompleted) { yield return null; }
            }
            if (dmg != null) {
                var ad = dmg.EnqueueRepair();
                if (ad != null) {
                    if (A.State.Adverts.Take(viewer.Being.Id, ad)) {
                        viewer.Being.Brain.TakeAd(ad);
                    }
                }
            }
        }
    }
}