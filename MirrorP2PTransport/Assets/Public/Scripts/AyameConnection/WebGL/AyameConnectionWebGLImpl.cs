#if UNITY_WEBGL
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System.IO;
using System.Reflection;

namespace Mirror.WebRTC
{
    public class AyameConnectionWebGLImpl : IAyameConnectionImpl
    {
        static class Ayame
        {
            [DllImport("__Internal")]
            public static extern void Connect(string signalingUrl, string signalingKey, string roomId, string dataChannelLabel, int dataChannelId);

            [DllImport("__Internal")]
            public static extern void Disconnect();

            [DllImport("__Internal")]
            public static extern void SendData(byte[] data, int size);

            [DllImport("__Internal")]
            public static extern int IsConnected();

            [DllImport("__Internal")]
            public static extern void InjectionJs(string url, string id);
        }

        public AyameConnectionImplConstants.OnMessageDelegate OnMessageHandler { get; set; }
        public AyameConnectionImplConstants.OnConnectedDelegate OnConnectedHandler { get; set; }
        public AyameConnectionImplConstants.OnDisconnectedDelegate OnDisconnectedHandler { get; set; }

        public void Connect(AyameConnectionImplConstants.ConnectSetting connectSetting)
        {
            UnityEngine.Debug.Log($"{this.GetType().Name}: {MethodBase.GetCurrentMethod().Name}");

            var eventReceiver = AyameEventReceiver.GetInstance();
            eventReceiver.ConnectedHandler += () => { this.OnConnectedHandler?.Invoke(connectSetting.DataChannelSettings[0]); };
            eventReceiver.DisconnectedHandler += () => { this.OnDisconnectedHandler?.Invoke(); };
            eventReceiver.MessageHandler += (data) => { this.OnMessageHandler?.Invoke(connectSetting.DataChannelSettings[0].Label, data); };

            var dataChannelLabel = connectSetting.DataChannelSettings[0].Label;
            var dataChannelId = connectSetting.DataChannelSettings[0].Id;
            Ayame.Connect(connectSetting.SignalingURL, connectSetting.SignalingKey, connectSetting.RoomId, dataChannelLabel, dataChannelId);
        }

        public void Disconnect()
        {
            Ayame.Disconnect();
        }

        public void SendMessage(string dataChannelLabel, byte[] data)
        {
            Ayame.SendData(data, data.Length);
        }

        public bool IsConnected(string dataChannelLabel)
        {
            return Ayame.IsConnected() == 1;
        }
    }

}
#endif
