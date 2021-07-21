using UnityEngine;
using TMPro;
using KL.UweenExt;
using UnityEngine.UI;
using System.Collections.Generic;
using Game.Data;
using Game.Components;

namespace Game.UI {
    public class TwitchInfoPanel : MonoBehaviour {
        #pragma warning disable 0649
        [SerializeField] private TMP_Text viewerCountText;
        #pragma warning restore 0649
        private CanvasGroup cg;
        private Button btn;
        void Start() {
            btn = GetComponent<Button>();
            btn.onClick.AddListener(OnClick);
            cg = GetComponent<CanvasGroup>();
            cg.alpha = 0;
            cg.interactable = false;
            cg.blocksRaycasts = false;
            viewerCountText.SetText("0");
        }

        void OnClick() {
            var viewers = new List<Being>();
            foreach (var being in A.State.Beings.All) {
                if (!being.IsActive) { continue; }
                if (being.HasComponent<TwitchViewerComp>()) {
                    viewers.Add(being);
                }
            }
            A.Sig.BeingsSelected.Send(viewers);
        }

        void OnTwitchActive(bool active) {
            cg.interactable = active;
            cg.blocksRaycasts = active;
            if (active) {
                TweenCGA.Add(gameObject, 1, 1);
            } else {
                cg.alpha = 0;
            }
        }

        void OnTwitchCountChanged(int count) {
            viewerCountText.SetText(count.ToString());
        }

        void OnEnable() {
            The.SysSig.TwitchActive.AddListener(OnTwitchActive, gameObject);
            The.SysSig.TwitchViewerCountChanged.AddListener(OnTwitchCountChanged, gameObject);
        }
        void OnDisable() {
            The.SysSig.TwitchActive.RemoveListener(OnTwitchActive);
            The.SysSig.TwitchViewerCountChanged.RemoveListener(OnTwitchCountChanged);
        }
    }
}