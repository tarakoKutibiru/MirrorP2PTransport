using System;
using UnityEngine;

namespace Mirror.WebRTC
{
    public class AyameEventReceiver : MonoBehaviour
    {
        public static readonly string Name = "AyameEventReceiver";

        public delegate void OnConnectedDelegate();
        public delegate void OnDisconnectedDelegate();
        public delegate void OnMessageDelegate(byte[] data);

        public OnConnectedDelegate ConnectedHandler;
        public OnDisconnectedDelegate DisconnectedHandler;
        public OnMessageDelegate MessageHandler;

        static AyameEventReceiver instance = default;

        public static AyameEventReceiver GetInstance()
        {
            if (AyameEventReceiver.instance != default) return AyameEventReceiver.instance;

            var gameObj = new GameObject();
            var receiver = gameObj.AddComponent<AyameEventReceiver>();
            gameObj.name = Name;
            AyameEventReceiver.instance = receiver;

            return receiver;
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

        public void OnMessage(string base64)
        {
            Debug.Log($"AyameReceive: {base64}");
            var data = Convert.FromBase64String(base64);

            Debug.Log($"data.length {data.Length}");
            MessageHandler?.Invoke(data);
        }
    }

}
