namespace Mirror.WebRTC
{
    public class AyameConnection
    {
#if UNITY_WEBGL
        IAyameConnectionImpl impl=new AyameConnectionWebGLImpl();
#else
        IAyameConnectionImpl impl = new AyameConnectionImpl();
#endif
        public OnMessageDelegate OnMessageHandler => this.impl.OnMessageHandler;
        public OnConnectedDelegate OnConnectedHandler { get => this.impl.OnConnectedHandler; set => this.impl.OnConnectedHandler = value; }
        public OnDisconnectedDelegate OnDisconnectedHandler => this.impl.OnDisconnectedHandler;

        public void Connect(string signalingURL, string signalingKey, string roomId, string[] dataChannelLabels, float timeOut)
        {
            this.impl.Connect(signalingURL, signalingKey, roomId, dataChannelLabels, timeOut);
        }

        public void Disconnect()
        {
            this.impl.Disconnect();
        }

        public void SendMessage(string dataChannelLabel, byte[] data)
        {
            this.impl.SendMessage(dataChannelLabel, data);
        }

        public bool IsConnected(string dataChannelLabel)
        {
            return this.impl.IsConnected(dataChannelLabel);
        }
    }
}
