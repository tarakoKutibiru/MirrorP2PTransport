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
            this.Connect();
        }

        public void Stop()
        {
            this.Disconnect();
        }

        void Connect()
        {
            if (this.IsConnected()) return;

            if (this.connection == default)
            {
                var connection = new MirrorP2PConnection(signalingURL: this.signalingURL, signalingKey: this.signalingKey, roomId: this.roomId);
                connection.OnConnectedHandler += this.OnConnected;
                connection.OnDisconnectedHandler += this.OnDisconnected;
                connection.OnMessageHandler += this.OnMessage;
                connection.OnRequestHandler += this.OnRequest;
                connection.Connect();
                this.connection = connection;
            }
            else
            {
                this.connection.Connect();
            }

            this.connectionStatus = ConnectionStatus.Connecting;
        }

        void Disconnect()
        {
            if (this.connection == default) return;

            this.connectionStatus = ConnectionStatus.Disconnecting;
            this.connection.Disconnect();
        }

        public bool Send(byte[] data)
        {
            if (!this.IsConnected())
            {
                UnityEngine.Debug.LogError("MirrorP2PClient Send Error.Not Connected.");

                return false;
            }

            this.connection.SendMessage(MirrorP2PMessage.CreateRawDataMessage(data));

            return true;
        }

        void OnMessage(MirrorP2PMessage message)
        {
            if (this.state == State.Stop) return;

            this.OnReceivedDataAction?.Invoke(message.rawData, 0);
        }

        void OnRequest(MirrorP2PMessage message)
        {
            switch (message.MessageType)
            {
                case MirrorP2PMessage.Type.ConnectedConfirmRequest:
                    {
                        this.connectionStatus = ConnectionStatus.Connected;
                        this.connection.SendResponce(MirrorP2PMessage.CreateConnectedConfirmResponce(message.Uid));
                        this.OnConnectedAction?.Invoke();
                        break;
                    }

                default:
                    break;
            }
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
            this.connectionStatus = ConnectionStatus.Disconnected;
            this.OnDisconnectedAction?.Invoke();
            this.connection = default;
        }

    }
}
