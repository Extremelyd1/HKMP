using HKMP.Game.Client;
using HKMP.Networking.Client;
using HKMP.UI.Component;
using HKMP.UI.Resources;
using HKMP.Util;
using UnityEngine;

namespace HKMP.UI {
    public class PingUI {
        // The margin between the text and the borders of the screen,
        // both horizontally and vertically
        private const int ScreenBorderMargin = 10;
        // The maximum width of the text component
        private const float TextWidth = 50f;
        // The maximum height of the text component
        private const float TextHeight = 25f;
        
        public PingUI(GameObject pingUiObject, ClientManager clientManager, NetClient netClient) {
            // Since we are initially not connected, we disable the object by default
            pingUiObject.SetActive(false);
            
            var pingTextComponent = new TextComponent(
                pingUiObject,
                new Vector2(
                    ScreenBorderMargin, Screen.height - ScreenBorderMargin),
                new Vector2(TextWidth, TextHeight),
                "",
                FontManager.UIFontRegular
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
                pingUiObject.SetActive(true);
            });
            clientManager.RegisterOnDisconnect(() => {
                pingUiObject.SetActive(false);
            });
        }
        
    }
}