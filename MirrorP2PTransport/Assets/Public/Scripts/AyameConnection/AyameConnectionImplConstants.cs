namespace Mirror.WebRTC
{
    public class AyameConnectionImplConstants
    {
        public delegate void OnMessageDelegate(string dataChannelLabel, byte[] rawData);
        public delegate void OnConnectedDelegate(DataChannelSetting dataChannelSetting);
        public delegate void OnDisconnectedDelegate();
        public class DataChannelSetting
        {
            public int Id;
            public string Label;
            public DataChannelSetting(int id, string label)
            {
                this.Id = id;
                this.Label = label;
            }
        }

        public class ConnectSetting
        {
            public string SignalingURL { get; private set; }
            public string SignalingKey { get; private set; }
            public string RoomId { get; private set; }
            public DataChannelSetting[] DataChannelSettings { get; private set; }
            public float TimeOut { get; private set; }
            public ConnectSetting(string signalingURL, string signalingKey, string roomId, DataChannelSetting[] dataChannelSettings, float timeOut)
            {
                this.SignalingURL = signalingURL;
                this.SignalingKey = signalingKey;
                this.RoomId = roomId;
                this.DataChannelSettings = dataChannelSettings;
                this.TimeOut = timeOut;
            }
        }
    }
}
