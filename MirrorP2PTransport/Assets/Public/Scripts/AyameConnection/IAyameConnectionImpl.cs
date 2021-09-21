using System;

namespace Mirror.WebRTC
{
    interface IAyameConnectionImpl<T> where T : IDisposable
    {
        AyameConnectionImplConstants.OnMessageDelegate OnMessageHandler { get; set; }
        AyameConnectionImplConstants.OnConnectedDelegate OnConnectedHandler { get; set; }
        AyameConnectionImplConstants.OnDisconnectedDelegate OnDisconnectedHandler { get; set; }

        void Connect(AyameConnectionImplConstants.ConnectSetting connectSetting);
        void Disconnect();
        void SendMessage(string dataChannelLabel, byte[] data);
        bool IsConnected(string dataChannelLabel);
    }
}
