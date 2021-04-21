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

        string roomId = "";
        public string RoomId { get => this.roomId; set => this.roomId = value; }

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

        void Connect()
        {
            if (this.IsConnected()) return;

            if (this.connection == default)
            {
                var connection = new MirrorP2PConnection(signalingURL: this.signalingURL, signalingKey: this.signalingKey, roomId: this.roomId);
                connection.onConnected += this.OnConnected;
                connection.onDisconnected += this.OnDisconnected;
                connection.onMessage += this.OnMessage;

                connection.Connect();

                this.connection = connection;
            }
            else
            {
                this.connection.Connect();
            }
        }

        void Disconnect()
        {
            if (this.connection == default) return;

            this.connection.Disconnect();
        }

        public bool Send(byte[] data)
        {
            if (!this.IsConnected())
            {
                UnityEngine.Debug.LogError("MirrorP2PClient Send Error.Not Connected.");

                return false;
            }

            this.connection.SendMessage(DataChannelLabelType.Mirror.ToString(), data);

            return true;
        }

        protected void OnMessage(string dataChannelLabel, byte[] bytes)
        {
            if (this.state == State.Stop) return;

            this.OnReceivedDataAction?.Invoke(bytes, 0);
        }

        public bool IsConnected()
        {
            if (this.connection == default) return false;
            if (!this.connection.IsConnectedAllDataChannel()) return false;

            return true;
        }

        protected void OnConnected()
        {
            this.OnConnectedAction?.Invoke();
        }

        protected void OnDisconnected()
        {
            this.OnDisconnectedAction?.Invoke();

            if (this.state == State.Runnning)
            {
                this.connection = default;
                this.Connect();
            }
            else if (this.state == State.Stop)
            {
                this.connection = default;
            }
        }
    }
}
