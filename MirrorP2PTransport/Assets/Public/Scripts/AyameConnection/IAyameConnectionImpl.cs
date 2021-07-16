using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using Unity.WebRTC;

namespace Mirror.WebRTC
{
    public delegate void OnMessageDelegate(string dataChannelLabel, byte[] rawData);
    public delegate void OnConnectedDelegate(RTCDataChannel dataChannel);
    public delegate void OnDisconnectedDelegate();

    interface IAyameConnectionImpl
    {
        OnMessageDelegate OnMessageHandler { get; set; }
        OnConnectedDelegate OnConnectedHandler { get; set; }
        OnDisconnectedDelegate OnDisconnectedHandler { get; set; }

        UniTask<List<int>> Connect(string signalingURL, string signalingKey, string roomId, string[] dataChannelLabels, float timeOut);
        void Disconnect();
        void SendMessage(string dataChannelLabel, byte[] data);
        bool IsConnected(string dataChannelLabel);
    }
}
