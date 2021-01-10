using System;
using Unity.WebRTC;

namespace Mirror.WebRTC
{
    public class MirrorP2PClient : Common
    {
        public event Action<Exception> OnReceivedErrorAction;
        public event Action<byte[], int> OnReceivedDataAction;
        public Action OnConnectedAction = null;
        public Action OnDisconnectedAction = null;

        public void Connect(string signalingURL, string signalingKey, string roomId)
        {
            this.signalingURL = signalingURL;
            this.signalingKey = signalingKey;
            this.roomId = roomId;

            base.Start();
        }

        public void Disconnect()
        {
            this.SendBye();
            base.Stop();
        }

        public bool Send(byte[] data)
        {
            return base.SendMessage(data);
        }

        protected override void OnMessage(RTCDataChannel channel, byte[] bytes)
        {
            base.OnMessage(channel, bytes);

            this.OnReceivedDataAction?.Invoke(bytes, 0);
        }

        public bool Connected()
        {
            return base.IsConnected();
        }

        protected override void OnConnected()
        {
            base.OnConnected();

            this.OnConnectedAction?.Invoke();
        }

        protected override void OnDisconnected()
        {
            base.OnDisconnected();

            this.OnDisconnectedAction?.Invoke();
        }
    }
}
