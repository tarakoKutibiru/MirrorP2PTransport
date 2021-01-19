using System;

namespace Mirror.WebRTC
{
    public class MirrorP2PClient : Common
    {
        public event Action<Exception> OnReceivedErrorAction;
        public event Action<byte[], int> OnReceivedDataAction;
        public Action OnConnectedAction = null;
        public Action OnDisconnectedAction = null;

        string signalingURL;
        string signalingKey;

        string roomId;
        public string RoomId { get; set; }

        MirrorP2PConnection connection = default;

        public MirrorP2PClient(string signalingURL, string signalingKey)
        {
            this.signalingURL = signalingURL;
            this.signalingKey = signalingKey;
        }

        public void Run()
        {
            this.state = State.Runnning;
            this.Connect();
        }

        public void Stop()
        {
            this.state = State.Stop;
            this.Disconnect();
        }

        public void Connect()
        {
            if (this.IsConnected()) return;

            var connection = new MirrorP2PConnection(signalingURL: this.signalingURL, signalingKey: this.signalingKey, roomId: this.roomId);
            connection.onConnected += this.OnConnected;
            connection.onDisconnected += this.OnDisconnected;
            connection.onMessage += this.OnMessage;

            connection.Connect();

            this.connection = connection;
        }

        public void Disconnect()
        {
            if (!this.IsConnected()) return;

            this.connection.Disconnect();
        }

        public bool Send(byte[] data)
        {
            if (!this.IsConnected()) return false;

            this.connection.SendMessage(data);

            return true;
        }

        protected void OnMessage(byte[] bytes)
        {
            if (this.state == State.Stop) return;

            this.OnReceivedDataAction?.Invoke(bytes, 0);
        }

        public bool IsConnected()
        {
            if (this.connection == default) return false;
            if (!this.connection.IsConnected()) return false;

            return true;
        }

        protected void OnConnected()
        {
            this.OnConnectedAction?.Invoke();
        }

        protected void OnDisconnected()
        {
            this.OnDisconnectedAction?.Invoke();

            // 再接続
            if (this.state == State.Runnning)
            {
                this.Connect();
            }
            else if (this.state == State.Stop)
            {
                this.connection = default;
            }
        }
    }
}
