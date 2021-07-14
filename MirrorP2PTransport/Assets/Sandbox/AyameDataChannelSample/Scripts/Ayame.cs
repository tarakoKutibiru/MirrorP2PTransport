using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System.IO;
using Mirror.WebRTC.Samples;
using UnityEngine.SceneManagement;

namespace Sandbox.AyameDataChannelSample
{
    public class Ayame : MonoBehaviour
    {
        public delegate void OnConnectedDelegate();
        public delegate void OnDisconnectedDelegate();
        public delegate void OnMessageDelegate(string message);

        public OnConnectedDelegate    ConnectedHandler;
        public OnDisconnectedDelegate DisconnectedHandler;
        public OnMessageDelegate      MessageHandler;

        #region javascript
        [DllImport("__Internal")]
        public static extern void Connect(string signalingUrl, string signalingKey, string roomId);

        [DllImport("__Internal")]
        public static extern void Disconnect();

        [DllImport("__Internal")]
        public static extern void SendData(string messsage);

        [DllImport("__Internal")]
        private static extern void InjectionJs(string url, string id);

        void Awake()
        {
            #if !UNITY_EDITOR && UNITY_WEBGL
            {
                // var url = "https://publicfilehost.web.app/Test.js";
                var url = Path.Combine(Application.streamingAssetsPath, "Ayame.js");
                var id  = "0";
                InjectionJs(url, id);
            }

            {
                var url = "https://cdn.jsdelivr.net/npm/@open-ayame/ayame-web-sdk@2020.3.0/dist/ayame.js";
                var id  = "1";
                InjectionJs(url, id);
            }
            #endif
        }

        public void OnEvent(string str)
        {
            Debug.Log(str);
            switch (str)
            {
                case "OnConnected":
                {
                    this.ConnectedHandler?.Invoke();
                    break;
                }
                case "OnDisconnected":
                {
                    this.DisconnectedHandler?.Invoke();
                    break;
                }
                default:
                {
                    break;
                }

            }
        }

        public void OnMessage(string str)
        {
            Debug.Log(str);
            MessageHandler?.Invoke(str);
        }
        #endregion
    }
}
