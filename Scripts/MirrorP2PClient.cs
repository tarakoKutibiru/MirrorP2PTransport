using Cysharp.Threading.Tasks;
using System;
using System.Threading;

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
        public string RoomId { get; set; }

        MirrorP2PConnection connection = default;

        CancellationTokenSource cts = default;

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
                var connection = new MirrorP2PConnection(signalingURL: this.signalingURL, signalingKey: this.signalingKey, roomId: this.RoomId);
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

            this.cts?.Cancel();
            this.connectionStatus = ConnectionStatus.Disconnecting;
            this.connection.Disconnect();
        }

        public bool Send(byte[] data)
        {
            if (this.state != State.Runnning) return false;
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

        }

        public bool IsConnected()
        {
            if (this.connectionStatus != ConnectionStatus.Connected) return false;
            if (this.connection == default) return false;
            if (!this.connection.IsConnected()) return false;

            return true;
        }

        protected void OnConnected()
        {
            this.cts?.Cancel();

            UniTask.Void(async () =>
            {
                this.cts = new CancellationTokenSource();
                var result = false;

                try
                {
                    while (!result && this.state == State.Runnning)
                    {
                        result = await this.connection.SendRequest(MirrorP2PMessage.CreateConnectedConfirmRequest(), this.cts.Token);
                    }
                }
                catch (OperationCanceledException ex)
                {
                    UnityEngine.Debug.Log(ex.Message);
                }
                finally
                {
                    this.cts = default;
                }

                if (!result) return;

                UnityEngine.Debug.Log($"Client OnConnected");

                if (this.connectionStatus != ConnectionStatus.Connected)
                {
                    this.connectionStatus = ConnectionStatus.Connected;
                    this.OnConnectedAction?.Invoke();
                }
            });
        }

        protected void OnDisconnected()
        {
            this.connectionStatus = ConnectionStatus.Disconnected;
            this.OnDisconnectedAction?.Invoke();
            this.connection = default;
        }

    }
}
