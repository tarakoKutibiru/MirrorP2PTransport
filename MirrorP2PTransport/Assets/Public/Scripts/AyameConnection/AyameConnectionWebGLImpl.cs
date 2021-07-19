#if UNITY_WEBGL
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mirror.WebRTC
{
    public class AyameConnectionWebGLImpl : IAyameConnectionImpl
    {
        public AyameConnectionImplConstants.OnMessageDelegate OnMessageHandler { get; set; }
        public AyameConnectionImplConstants.OnConnectedDelegate OnConnectedHandler { get; set; }
        public AyameConnectionImplConstants.OnDisconnectedDelegate OnDisconnectedHandler { get; set; }

        public void Connect(AyameConnectionImplConstants.ConnectSetting connectSetting)
        {
        }

        public void Disconnect()
        {

        }

        public void SendMessage(string dataChannelLabel, byte[] data)
        {

        }

        public bool IsConnected(string dataChannelLabel)
        {
            return false;
        }
    }

}
#endif
