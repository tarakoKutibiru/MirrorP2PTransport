using System;
using System.Reflection;
using Cysharp.Threading.Tasks;

namespace Mirror.WebRTC
{
    public class MirrorP2PServer : Common
    {
        static readonly int connectionId = 1;
        static readonly int channelId    = 0;

        public event Action<int>              OnConnectedAction; // connectionId
        public event Action<int, byte[], int> OnReceivedDataAction; // connectionId, data, channnelId
        public event Action<int>              OnDisconnectedAction; // connectionId
        public event Action<int, Exception>   OnReceivedErrorAction; // connectionId

        string signalingURL;
        string signalingKey;
        string roomId;

        MirrorP2PConnection connection  = default;
        MirrorP2PConnection connectionA = default;
        MirrorP2PConnection connectionB = default;

        public void Start(string signalingURL, string signalingKey, string roomId)
        {
            if (this.state == State.Runnning) return;

            this.state = State.Runnning;

            this.signalingURL = signalingURL;
            this.signalingKey = signalingKey;
            this.roomId       = roomId;

            this.Connect();
        }

        public void Stop()
        {
            if (this.state == State.Stop) return;

            this.state = State.Stop;
            this.Disconnect(MirrorP2PServer.channelId);
        }

        public bool Send(int connectionId, byte[] data)
        {
            if (!this.IsConnected()) return false;

            this.connection.SendMessage(MirrorP2PMessage.Create<RawData>(new RawData(data)));

            return true;
        }

        void Connect()
        {
            UnityEngine.Debug.Log($"{this.GetType().Name}: {MethodBase.GetCurrentMethod().Name}");
            this.connectionStatus = ConnectionStatus.Connecting;

            if (this.connection == default)
            {
                var connectionA = new MirrorP2PConnection(signalingURL: signalingURL, signalingKey: signalingKey, roomId: $"{roomId}_A");
                connectionA.OnConnectedHandler = () =>
                {
                    UnityEngine.Debug.Log("OnConnected");

                    var connectionB = new MirrorP2PConnection(signalingURL: signalingURL, signalingKey: signalingKey, roomId: $"{roomId}_B");
                    connectionB.OnConnectedHandler = () =>
                    {
                        UnityEngine.Debug.Log("OnConnected");
                        UniTask.Void(async() =>
                        {
                            while (true)
                            {
                                await UniTask.Delay(TimeSpan.FromSeconds(1));
                                var message = new AnyString("World");
                                connectionB.SendMessage(MirrorP2PMessage.Create<AnyString>(message));
                            }
                        });
                    };
                    connectionB.OnAnyMessageHandler = (type, message) =>
                    {
                        if (type == typeof(AnyString))
                        {
                            var anyString = message as AnyString;;
                            UnityEngine.Debug.Log(anyString.message);
                        }
                    };
                    connectionB.Connect();
                    this.connectionB = connectionB;

                    UniTask.Void(async() =>
                    {
                        while (true)
                        {
                            await UniTask.Delay(TimeSpan.FromSeconds(1));
                            var message = new AnyString("Hello");
                            connectionA.SendMessage(MirrorP2PMessage.Create<AnyString>(message));
                        }
                    });
                };
                connectionA.OnAnyMessageHandler = (type, message) =>
                {
                    if (type == typeof(AnyString))
                    {
                        var anyString = message as AnyString;;
                        UnityEngine.Debug.Log(anyString.message);
                    }
                };
                connectionA.Connect();
                this.connectionA = connectionA;
            }
            else
            {
                connection.Connect();
            }
        }

        public bool Disconnect(int connectionId)
        {
            if (this.connectionA != default)
            {
                this.connectionA.Disconnect();
                this.connectionA.OnAnyMessageHandler = default;
                this.connectionA.OnConnectedHandler  = default;
                this.connectionA = default;
            }

            if (this.connectionB != default)
            {
                this.connectionB.Disconnect();
                this.connectionB.OnAnyMessageHandler = default;
                this.connectionB.OnConnectedHandler  = default;
                this.connectionB = default;
            }

            return true;
        }

        public bool IsRunning()
        {
            if (this.state != State.Runnning) return false;

            return true;
        }

        public bool IsConnected()
        {
            if (this.connectionStatus != ConnectionStatus.Connected) return false;
            if (this.connection == default) return false;
            if (!this.connection.IsConnected()) return false;

            return true;
        }

        void OnMessage(RawData rawData)
        {
            if (this.connectionStatus != ConnectionStatus.Connected) return;
            this.OnReceivedDataAction?.Invoke(MirrorP2PServer.connectionId, rawData.rawData, MirrorP2PServer.channelId);
        }

        void OnRequest(Type type, IRequest request)
        {
            if (type == typeof(ConnectedConfirmRequest))
            {
                this.connection.SendResponce(MirrorP2PMessage.Create<ConnectedConfirmResponce>(new ConnectedConfirmResponce(request as ConnectedConfirmRequest)));
                UnityEngine.Debug.Log($"Server OnConnected");
                if (this.connectionStatus == ConnectionStatus.Connected) return;
                this.connectionStatus = ConnectionStatus.Connected;
                this.OnConnectedAction?.Invoke(MirrorP2PServer.connectionId);
            }
        }

        void OnConnected()
        {

        }

        void OnDisconnected()
        {
            this.connectionStatus = ConnectionStatus.Disconnected;
            this.Disconnect(MirrorP2PServer.connectionId);

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
