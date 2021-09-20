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
        public string BaseRoomId { get; set; }
        string roomId = default;

        MirrorP2PConnection baseConnection = default;

        MirrorP2PConnection connection = default;
        Common.ConnectionStatus connectionStatus = ConnectionStatus.Disconnected;

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

            if (string.IsNullOrEmpty(this.roomId))
            {
                var connection = new MirrorP2PConnection(signalingURL: this.signalingURL, signalingKey: this.signalingKey, roomId: this.BaseRoomId);
                connection.OnConnectedHandler = () =>
                {
                    UniTask.Void(async () =>
                    {
                        // 接続確認
                        var connectedConfirmResponse = await this.Request<ConnectedConfirmRequest, ConnectedConfirmResponce>(this.baseConnection, new ConnectedConfirmRequest());
                        if (connectedConfirmResponse == default) return;
                        UnityEngine.Debug.Log("###ConnectedConfirm");

                        // 個別のroomIdを取得
                        var connectServerResponse = await this.Request<ConnectServerRequest, ConnectServerResponse>(this.baseConnection, new ConnectServerRequest());
                        if (connectServerResponse == default) return;
                        this.roomId = connectServerResponse.roomId;
                        UnityEngine.Debug.Log($"###connectServerResponse {this.roomId}");

                        // 改めて個別のroomIdでServer(Host)と接続する
                        this.Connect(this.roomId);
                    });
                };
                connection.OnDisconnectedHandler = () =>
                {
                    this.baseConnection.OnConnectedHandler = default;
                    this.baseConnection.Disconnect();
                    this.baseConnection = default;
                };
                this.baseConnection = connection;
                this.baseConnection.Connect();
            }
            else if (this.connection == default)
            {
                this.Connect(this.roomId);
            }
            else
            {
                this.connection.Connect();
            }

            this.connectionStatus = ConnectionStatus.Connecting;
        }

        void Connect(string roomId)
        {
            if (this.IsConnected()) return;

            if (this.connection == default)
            {
                var connection = new MirrorP2PConnection(signalingURL: this.signalingURL, signalingKey: this.signalingKey, roomId: roomId);
                connection.OnConnectedHandler = this.OnConnected;
                connection.OnDisconnectedHandler = this.OnDisconnected;
                connection.OnMessageHandler = this.OnMessage;
                connection.OnRequestHandler = this.OnRequest;
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
            this.cts?.Cancel();

            if (this.baseConnection != default)
            {
                this.baseConnection.Disconnect();
                this.baseConnection.ClearEvents();
                this.baseConnection = default;
            }

            this.connectionStatus = ConnectionStatus.Disconnecting;

            if (this.connection != default)
            {
                this.connection.Disconnect();
                this.connection.ClearEvents();
                this.connection = default;
            }

            this.roomId = string.Empty;
        }

        public bool Send(byte[] data)
        {
            if (this.state != State.Runnning) return false;
            if (!this.IsConnected())
            {
                UnityEngine.Debug.LogError("MirrorP2PClient Send Error.Not Connected.");

                return false;
            }

            this.connection.SendMessage(MirrorP2PMessage.Create<RawData>(new RawData(data)));

            return true;
        }

        void OnMessage(RawData rawData)
        {
            if (this.state == State.Stop) return;

            this.OnReceivedDataAction?.Invoke(rawData.rawData, 0);
        }

        void OnRequest(Type type, IRequest request)
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
                var response = await this.Request<ConnectedConfirmRequest, ConnectedConfirmResponce>(this.connection, new ConnectedConfirmRequest());
                if (response == default) return;

                UnityEngine.Debug.Log($"Client OnConnected");

                if (this.connectionStatus != ConnectionStatus.Connected)
                {
                    this.connectionStatus = ConnectionStatus.Connected;
                    this.OnConnectedAction?.Invoke();
                }
            });
        }

        async UniTask<U> Request<T, U>(MirrorP2PConnection connection, T t) where T : class, IRequest where U : class, IResponse
        {
            this.cts = new CancellationTokenSource();
            U response = default;

            try
            {
                while (response == default && this.state == State.Runnning)
                {
                    response = await connection.SendRequest(t, this.cts.Token) as U;
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

            return response;
        }

        protected void OnDisconnected()
        {
            this.connectionStatus = ConnectionStatus.Disconnected;
            this.OnDisconnectedAction?.Invoke();
            this.connection = default;
        }

    }
}
