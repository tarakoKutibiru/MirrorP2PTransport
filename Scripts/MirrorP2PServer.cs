using Cysharp.Threading.Tasks;
using System;
using System.Threading;

namespace Mirror.WebRTC
{
    public class MirrorP2PServer : Common
    {
        static readonly int connectionId = 1;
        static readonly int channelId = 0;

        public event Action<int> OnConnectedAction; // connectionId
        public event Action<int, byte[], int> OnReceivedDataAction; // connectionId, data, channnelId
        public event Action<int> OnDisconnectedAction; // connectionId
        public event Action<int, Exception> OnReceivedErrorAction; // connectionId

        string signalingURL;
        string signalingKey;
        string roomId;

        MirrorP2PConnection connection = default;

        public void Start(string signalingURL, string signalingKey, string roomId)
        {
            if (this.state == State.Runnning) return;

            this.state = State.Runnning;

            this.signalingURL = signalingURL;
            this.signalingKey = signalingKey;
            this.roomId = roomId;

            this.Connect();
        }

        public void Stop()
        {
            if (this.state == State.Stop) return;

            this.state = State.Stop;
            this.connection.Disconnect();
        }

        public bool Send(int connectionId, byte[] data)
        {
            if (!this.IsConnected()) return false;

            this.connection.SendMessage(MirrorP2PMessage.CreateRawDataMessage(data));

            return true;
        }

        void Connect()
        {
            if (this.connection == default)
            {
                var connection = new MirrorP2PConnection(signalingURL: signalingURL, signalingKey: signalingKey, roomId: roomId);

                connection.OnConnectedHandler += this.OnConnected;
                connection.OnDisconnectedHandler += this.OnDisconnected;
                connection.OnMessageHandler += this.OnMessage;

                connection.Connect();

                this.connection = connection;
            }
            else
            {
                connection.Connect();
            }
        }

        public bool Disconnect(int connectionId)
        {
            if (!this.IsConnected()) return false;

            this.connection.Disconnect();

            return true;
        }

        public bool IsRunning()
        {
            if (this.state != State.Runnning) return false;

            return true;
        }

        public bool IsConnected()
        {
            if (this.connection == default) return false;
            if (!this.connection.IsConnected()) return false;

            return true;
        }

        void OnMessage(MirrorP2PMessage message)
        {
            this.OnReceivedDataAction?.Invoke(MirrorP2PServer.connectionId, message.ToPayload(), MirrorP2PServer.channelId);
        }

        void OnConnected()
        {
            UnityEngine.Debug.Log($"Server OnConnected {MirrorP2PServer.connectionId}");

            UniTask.Void(async () =>
            {
                var ct = new CancellationTokenSource(); // TODO:
                var result = false;
                while (!result && this.state == State.Runnning)
                {
                    result = await this.connection.SendRequest(MirrorP2PMessage.CreateConnectedConfirmRequest(), ct.Token);
                }

                this.OnConnectedAction?.Invoke(MirrorP2PServer.connectionId);
            });
        }

        void OnDisconnected()
        {
            this.OnDisconnectedAction?.Invoke(MirrorP2PServer.connectionId);
            UnityEngine.Debug.Log("MirrorP2PServer:OnDisconnected");
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
