using HKMP.Game.Client;
using HKMP.Game.Settings;
using HKMP.Networking.Client;
using HKMP.UI.Component;
using HKMP.UI.Resources;
using HKMP.Util;
using UnityEngine;

namespace HKMP.UI {
    public class PingUI {
        // The margin between the text and the borders of the screen,
        // both horizontally and vertically
        private const float ScreenBorderMargin = 20f;
        // The margin between the icon and the text
        private const float IconTextMargin = 25f;
        // The maximum width of the text component
        private const float TextWidth = 50f;
        // The maximum height of the text component
        private const float TextHeight = 25f;
        // The size (width and height) of the icon displayed in front of the text
        private const float IconSize = 20f;

        private readonly GameObject _pingUiObject;
        private readonly ModSettings _modSettings;
        private readonly NetClient _netClient;

        public PingUI(
            GameObject pingUiObject,
            ModSettings modSettings,
            ClientManager clientManager, 
            NetClient netClient
        ) {
            _pingUiObject = pingUiObject;
            _modSettings = modSettings;
            _netClient = netClient;
            
            // Since we are initially not connected, we disable the object by default
            pingUiObject.SetActive(false);

            new ImageComponent(
                pingUiObject,
                new Vector2(
                    ScreenBorderMargin, Screen.height - ScreenBorderMargin),
                new Vector2(IconSize, IconSize),
                TextureManager.NetworkIcon
            );

            var pingTextComponent = new TextComponent(
                pingUiObject,
                new Vector2(
                    ScreenBorderMargin + IconSize + IconTextMargin, Screen.height - ScreenBorderMargin - 1),
                new Vector2(TextWidth, TextHeight),
                "",
                FontManager.UIFontRegular,
                15,
                alignment: TextAnchor.MiddleLeft
            );

            // Register on update so we can set the text to the latest average RTT
            MonoBehaviourUtil.Instance.OnUpdateEvent += () => {
                if (!netClient.IsConnected) {
                    return;
                }
                
                pingTextComponent.SetText(netClient.UpdateManager.AverageRtt.ToString());
            };
            
            // Register on connect and disconnect so we can show/hide the ping accordingly
            clientManager.RegisterOnConnect(() => {
                SetEnabled(true);
            });
            clientManager.RegisterOnDisconnect(() => {
                SetEnabled(false);
            });
        }

        public void SetEnabled(bool enabled) {
            _pingUiObject.SetActive(enabled && _netClient.IsConnected && _modSettings.DisplayPing);
        }
        
    }
}