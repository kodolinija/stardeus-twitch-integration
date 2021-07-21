using Game.Data;
using Game.Components;
using KL.Commands;
using UnityEngine;
using Game.Systems;
using KL.I18N;
using KL.Collections;
using Game.Data.Twitch;
using System.Collections;
using KL.Integrations.Twitch;
using KL.Randomness;
using Game.Constants;

namespace Game.Commands.Twitch {
    public class CmdTwitchSpawnViewer : AsyncCommand {
        public override bool IsUnique => false;
        private const string cmd = "!join";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType
            .SubsystemRegistration)]
        protected static void Register() {
            TwitchSys.RegisterCommand(cmd, 
                (msg, viewer) => new CmdTwitchSpawnViewer(viewer, msg),
                "twitch.cmd.join.help");
        }

        public override string Id => "twitch_!join";
        private TwitchViewer viewer;

        public CmdTwitchSpawnViewer(TwitchViewer viewer, TwitchMessage msg) {
            this.viewer = viewer;
            if (msg != null) {
                // Update roles
                A.Sys.Twitch.SetInitialRoles(viewer, msg);
            }
        }

        public override IEnumerator ExecuteAsync() {
            if (viewer.HasRole("play")) { yield break; }
            yield return Ready.WaitForInput(this);
            // just to be sure
            viewer.AddRole("play");
            A.Sys.Twitch.ViewerJoined(viewer);
            var position = A.State.BB.Get(
                BBKey.CameraPosition, A.State.Grid.Center)
                    + Rng.UInsideUnitCircle() * 3f;
            var def = The.Defs.Get(DefIds.BeingsDrone01);
            var being = A.State.Beings.Create(position, def);
            var viewerComp = new TwitchViewerComp();
            A.State.Components.AddRuntime(being, viewerComp);
            viewerComp.Init(viewer);
        }
    }
}