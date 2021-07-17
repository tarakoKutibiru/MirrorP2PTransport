namespace Mirror.WebRTC
{
    public class AyameConnection
    {
        public delegate void OnMessageDelegate(byte[] rawData);
        public delegate void OnConnectedDelegate();
        public delegate void OnDisconnectedDelegate();

#if UNITY_WEBGL
        IAyameConnectionImpl impl=new AyameConnectionWebGLImpl();
#else
        IAyameConnectionImpl impl = new AyameConnectionImpl();
#endif
        public OnMessageDelegate OnMessageHandler;
        public OnConnectedDelegate OnConnectedHandler;
        public OnDisconnectedDelegate OnDisconnectedHandler;

        static readonly AyameConnectionImplConstants.DataChannelSetting DataChannelSetting = new AyameConnectionImplConstants.DataChannelSetting(0, "mirror");

        public void Connect(string signalingURL, string signalingKey, string roomId, float timeOut)
        {
            this.impl.OnConnectedHandler += this.OnConnected;
            this.impl.OnMessageHandler += (string dataChannelLabel, byte[] rawData) => { this.OnMessageHandler?.Invoke(rawData); };
            this.impl.OnDisconnectedHandler += () => { this.OnDisconnectedHandler?.Invoke(); };

            var dataChannelSettings = new AyameConnectionImplConstants.DataChannelSetting[] { DataChannelSetting };
            var settings = new AyameConnectionImplConstants.ConnectSetting(signalingURL, signalingKey, roomId, dataChannelSettings, timeOut);
            this.impl.Connect(settings);
        }

        public void Disconnect()
        {
            this.impl.Disconnect();
        }

        public void SendMessage(byte[] data)
        {
            this.impl.SendMessage(DataChannelSetting.Label, data);
        }

        public bool IsConnected()
        {
            return this.impl.IsConnected(DataChannelSetting.Label);
        }

        void OnConnected(AyameConnectionImplConstants.DataChannelSetting dataChannelSetting)
        {
            if (DataChannelSetting.Label != dataChannelSetting.Label) return;
            if (DataChannelSetting.Id != dataChannelSetting.Id) return;

            this.OnConnectedHandler?.Invoke();
        }
    }
}
