using System.Collections;
using System.Collections.Generic;
using Game.Commands;
using Game.Commands.Twitch;
using Game.Constants;
using Game.Data;
using Game.Data.Twitch;
using Game.Systems;
using Game.Utils;
using KL.I18N;
using KL.Randomness;
using KL.Text;
using UnityEngine;

namespace Game.Components {
    public sealed class TwitchViewerComp : BaseComponent<TwitchViewerComp>, 
            IUIDataProvider {

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.
            SubsystemRegistration)]
        static void Register() {
            AddComponentPrototype(new TwitchViewerComp());
        }

        public override bool IsRuntime => true;

        public TwitchViewer Viewer;
        private UDB dataBlock;

        private float explosionTimeout;
        private bool isExplosionPending;
        public bool IsExplosionPending => isExplosionPending;

        public override void OnLateReady(bool wasLoaded) {
            if (wasLoaded) {
                Being.Brain.DropCurrentAd();
                A.State.Beings.Destroy(Being, false);
            }
        }

        public override string ToString() {
            return $" * TwitchViewerComp";
        }

        public void Init(TwitchViewer viewer) {
            Viewer = viewer;
            viewer.Being = Being;
            Being.Name.SetName(Viewer.Name);
            ApplyRoleEffects();
        }

        public IEnumerator CountdownToExplosion() {
            var text = GameText.Create("BigWarningMessage", 
                $"{Viewer.Name}: 10", 
                Being.Graphics.Position + CmdTwitchSay.TextOffset);
            explosionTimeout = 30;
            isExplosionPending = true;
            while (explosionTimeout > 0) {
                explosionTimeout -= Time.unscaledDeltaTime;
                if (isExplosionPending) {
                    text.SetText(string.Format("{0:0.0}", explosionTimeout));
                } else {
                    text.SetText("twitch.cmd.explode.defused".T());
                }
                text.MoveTo(Being.Graphics.Position + CmdTwitchSay.TextOffset);
                yield return null;
            }
            text.Destroy();
            if (isExplosionPending) {
                Explode();
            }
        }

        private void Explode() {
            A.State.Beings.Destroy(Being);
            A.CmdQ.Enqueue(new CmdCreateExplosion(
                Being.Position,
                Rng.URange(3, 10), 
                Rng.URange(100, 300)));
            A.CmdQ.Enqueue(new CmdTwitchLeave(Viewer));
        }

        private void ApplyRoleEffects() {
            if (!Viewer.HasRole("play")) {
                A.CmdQ.Enqueue(new CmdTwitchLeave(Viewer));
                return;
            }
            Being.Brain.IsSpectator = !Viewer.HasRole("work");
        }

        public UDB GetUIBlock() {
            if (dataBlock == null) {
                dataBlock = UDB.Create(this, 
                    UDBT.IText,
                    IconId.CTwitchGlitch,
                    Viewer.Name);
                UpdateUIBlock(false);
            }
            return dataBlock;
        }

        public void UpdateUIBlock(bool wasUpdated) {
            if (dataBlock == null) { return; }
            dataBlock.Text = "twitch.reputation".T(Viewer.Reputation);
            dataBlock.WasUpdated = wasUpdated;
        }

        public List<UDB> GetUIDetails() {
            var list = new List<UDB>();
            if (isExplosionPending) {
                list.Add(UDB.Create(this,
                    UDBT.DBtnLong,
                    IconId.CWarning,
                    "twitch.cmd.explode.defuse".T()).WithLongPressFunction(() => {
                        isExplosionPending = false;
                        explosionTimeout = 5;
                        dataBlock.NeedsCompleteRebuild = true;
                        UpdateUIBlock(true);
                    }));
            }
            if (Viewer.IsBroadcaster || Viewer.IsVIP || Viewer.IsMod) {
                list.Add(UDB.Create(this, 
                    UDBT.DText,
                    IconId.CInfo,
                    Viewer.Privileges));
            }
            list.Add(UDB.Create(this,
                UDBT.DBtn,
                IconId.WArrowUp,
                "twitch.reputation.add".T(100)).WithClickFunction(() => {
                    Viewer.AddReputation(100);
                    UpdateUIBlock(true);
                }));
            foreach (var role in A.Sys.Twitch.Roles) {
                var roleName = role.Key;
                var roleBlock = UDB.Create(this, 
                    UDBT.DBtn,
                    Viewer.HasRole(roleName) ? IconId.CCheck : IconId.CCross,
                    $"role.{roleName}".T());
                    roleBlock.WithClickFunction(() => {
                        if (Viewer.HasRole(roleName)) {
                            Viewer.RemoveRole(roleName);
                            roleBlock.UpdateIcon(IconId.CCross);
                        } else {
                            Viewer.AddRole(roleName);
                            roleBlock.UpdateIcon(IconId.CCheck);
                        }
                        ApplyRoleEffects();
                        roleBlock.WasUpdated = true;
                    });
                list.Add(roleBlock);
            }
            return list;
        }
    }

}