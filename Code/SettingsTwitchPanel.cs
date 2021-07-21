using Game.Constants;
using KL.I18N;
using UnityEngine;
using TMPro;
using Game.Utils;
using Game.Systems;

namespace Game.UI {
    public class SettingsTwitchPanel : MonoBehaviour, IUIPanel {

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType
            .SubsystemRegistration)]
        private static void Register() {
            Panels.AddSettingsPanel("menu.settings.twitch", 
                new PanelDescriptor(typeof(SettingsTwitchPanel), true,
                    int.MinValue + 5, IconId.WTwitch));
        }

        private TMP_InputField channelNameInput;
        private TMP_InputField botNameInput;
        private TMP_Text oauthLabel;

        void Start() {
            UIBuilder.CreateText("UIHeadingWidget", "menu.settings.twitch".T(),
                transform);

            var twitchActive = UIBuilder.CreateToggle("UIToggleWidget", transform, 
                Pref.GTwitchIsActive.T(), The.Prefs.GetBool(
                        Pref.GTwitchIsActive, true), (v) => {
                    The.Prefs.SetBool(Pref.GTwitchIsActive, v);
                });
            UIBuilder.AddTooltip(twitchActive.gameObject, "twitch.active.toggle.tip".T());

            UIBuilder.CreateText("UILabelWidget", 
                Pref.GTwitchChannel.T(), transform);

            channelNameInput = UIBuilder.CreateInputField(
                "UITextInputWidget", The.Prefs.GetString(
                    Pref.GTwitchChannel, ""), 
                    $"{Pref.GTwitchChannel}.placeholder".T(), transform);
            channelNameInput.onValueChanged.AddListener(OnChannelChanged);
            UIBuilder.AddTooltip(channelNameInput.gameObject, "twitch.channel.name.tip".T());

            UIBuilder.CreateText("UILabelWidget", 
                Pref.GTwitchBotName.T(), transform);

            var botName = UIBuilder.CreateInputFieldWithButton(
                    "UITextInputWithButtonWidget", 
                        The.Prefs.GetString(Pref.GTwitchBotName, ""), 
                        $"{Pref.GTwitchBotName}.placeholder".T(),
                            "copy.channel.name".T(), 
                    CopyChannelNameToBotName, transform);
            botNameInput = botName.Input;
            botNameInput.onValueChanged.AddListener(OnBotNameChanged);
            UIBuilder.AddTooltip(botNameInput.gameObject, "twitch.bot.name.tip".T());

            oauthLabel = UIBuilder.CreateText("UILabelWidget", "", transform);
            SetOauthLabel();

            var oauthGen = UIBuilder.CreateButtonLongPress("UIMenuButtonLongPressWidget", 
                "twitch.bot.oauth.generate".T(), Consts.LongPressTime, () => {
                    UIPopupChoiceWidget.Spawn(IconId.CInfo, 
                        "twitch.bot.oauth.generate.title".T(),
                            "twitch.bot.oauth.generate.message".T(
                                Consts.TwitchTokenGeneratorURL), () => {
                                    Application.OpenURL(
                                        Consts.TwitchTokenGeneratorURL);
                            }, null, "open.url");
                }, transform);
            UIBuilder.AddTooltip(oauthGen.gameObject, "twitch.oauth.gen.tip".T());

            UIBuilder.CreateButtonLongPress("UIMenuButtonLongPressWidget", 
                "paste.from.clipboard".T(), Consts.LongPressTime, () => {
                    var oauth = GUIUtility.systemCopyBuffer;
                    if (string.IsNullOrWhiteSpace(oauth)) {
                        UIPopupWidget.Spawn(IconId.CWarning, 
                            "failed.pasting.value".T(),
                                "clipboard.is.empty".T());
                    } else {
                        The.Prefs.SetString(Pref.GTwitchBotOAuth, 
                            oauth.Trim());
                    }
                }, transform);

            UIBuilder.CreateButtonLongPress("UIMenuButtonLongPressWidget", 
                "copy.to.clipboard".T(), Consts.LongPressTime, () => {
                    var val = The.Prefs.GetString(
                        Pref.GTwitchBotOAuth, null);
                    if (string.IsNullOrWhiteSpace(val)) {
                        UIPopupWidget.Spawn(IconId.CWarning, 
                            "failed.copying.value".T(),
                                "value.is.empty".T());
                    }
                    GUIUtility.systemCopyBuffer = 
                        The.Prefs.GetString(Pref.GTwitchBotOAuth, "");
                }, transform);

            var testCfg = UIBuilder.CreateButton("UIMenuButtonWidget", 
                "test.configuration".T(), TestConfiguration, transform);
            UIBuilder.AddTooltip(testCfg.gameObject, "twitch.test.cfg.tip".T());
        }

        private void TestConfiguration() {
            if (!The.Prefs.GetBool(Pref.GTwitchIsActive, false)) {
                UIPopupWidget.Spawn(IconId.CWarning, 
                    "twitch.settings.invalid".T(),
                        "twitch.integration.disabled".T());
                return;
            }
            var chanName = The.Prefs.GetString(
                    Pref.GTwitchChannel, null);
            if (string.IsNullOrWhiteSpace(chanName)) {
                UIPopupWidget.Spawn(IconId.CWarning, 
                    "twitch.settings.invalid".T(),
                        "twitch.channel.name.missing".T());
                return;
            }
            var botName = The.Prefs.GetString(
                    Pref.GTwitchBotName, null);
            if (string.IsNullOrWhiteSpace(botName)) {
                UIPopupWidget.Spawn(IconId.CWarning, 
                    "twitch.settings.invalid".T(),
                        "twitch.bot.name.missing".T());
                return;
            }
            var botOauth = The.Prefs.GetString(
                    Pref.GTwitchBotOAuth, null);
            if (string.IsNullOrWhiteSpace(botOauth)) {
                UIPopupWidget.Spawn(IconId.CWarning, 
                    "twitch.settings.invalid".T(),
                        "twitch.bot.oauth.missing".T(
                            Consts.TwitchTokenGeneratorURL));
                return;
            }
            TwitchSys.TestConfiguration();
            UIPopupWidget.Spawn(IconId.CInfo, 
                "twitch.settings".T(),
                    "twitch.test.message.sent".T());
        }

        private void SetOauthLabel() {
            if (string.IsNullOrWhiteSpace(
                    The.Prefs.GetString(Pref.GTwitchBotOAuth, null))) {
                oauthLabel.text = string.Format("{0} [{1}]", 
                    Pref.GTwitchBotOAuth.T(), "missing".T());
            } else {
                oauthLabel.text = string.Format("{0} [{1}]", 
                    Pref.GTwitchBotOAuth.T(), "present".T());
            }
        }

        private void CopyChannelNameToBotName() {
            if (string.IsNullOrWhiteSpace(channelNameInput.text)) {
                UIPopupWidget.Spawn(IconId.CWarning, 
                    "failed.copying.value".T(),
                        "value.is.empty".T());
            }
            botNameInput.text = channelNameInput.text;
        }

        private void OnChannelChanged(string text) { 
            The.Prefs.SetString(Pref.GTwitchChannel, text);
        }

        private void OnBotNameChanged(string text) {
            The.Prefs.SetString(Pref.GTwitchBotName, text);
        }

        public void LoadArgs(PanelDescriptor desc) { }

        public void SetActive(bool on) {
            gameObject.SetActive(on);
            if (!on) {
                The.SaveGlobalSettings();
            }
        }
    }
}