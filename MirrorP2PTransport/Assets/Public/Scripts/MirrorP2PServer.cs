using System;
using System.Collections.Generic;
using System.Reflection;

namespace Mirror.WebRTC
{
    public class MirrorP2PServer : Common
    {
        static readonly int channelId = 0;

        public event Action<int> OnConnectedAction; // connectionId
        public event Action<int, byte[], int> OnReceivedDataAction; // connectionId, data, channnelId
        public event Action<int> OnDisconnectedAction; // connectionId
        public event Action<int, Exception> OnReceivedErrorAction; // connectionId

        string signalingURL;
        string signalingKey;
        string roomId;

        MirrorP2PConnection baseConnection = default;
        ConnectionStatus BaseConnectionStatus { get; set; } = ConnectionStatus.Disconnected;

        Dictionary<int, MirrorP2PConnection> connections = new Dictionary<int, MirrorP2PConnection>();
        Dictionary<int, ConnectionStatus> connectionStatus = new Dictionary<int, ConnectionStatus>();

        public void Start(string signalingURL, string signalingKey, string roomId)
        {
            if (this.state == State.Runnning) return;

            this.state = State.Runnning;

            this.signalingURL = signalingURL;
            this.signalingKey = signalingKey;
            this.roomId = roomId;

            this.baseConnection = new MirrorP2PConnection(signalingURL: signalingURL, signalingKey: signalingKey, roomId: roomId);
            baseConnection.OnRequestHandler = (Type type, IRequest request) =>
            {
                if (type == typeof(ConnectedConfirmRequest))
                {
                    this.baseConnection.SendResponce(MirrorP2PMessage.Create<ConnectedConfirmResponce>(new ConnectedConfirmResponce(request as ConnectedConfirmRequest)));
                }
                else if (type == typeof(ConnectServerRequest))
                {
                    var newConnectionId = this.connectionStatus.Count + 1; // TODO: ユニーク？
                    this.baseConnection.SendResponce(MirrorP2PMessage.Create<ConnectServerResponse>(new ConnectServerResponse(request as ConnectServerRequest, this.GenerateRoomId(newConnectionId))));
                    this.Connect(newConnectionId);
                }
            };
            this.baseConnection.Connect();
        }

        public void Stop()
        {
            if (this.state == State.Stop) return;

            this.state = State.Stop;

            this.baseConnection.OnRequestHandler = default;
            this.baseConnection.Disconnect();
            this.baseConnection = default;
            this.DisconnectAll();
        }

        public bool Send(int connectionId, byte[] data)
        {
            if (!this.IsConnected(connectionId)) return false;
            this.connections[connectionId].SendMessage(MirrorP2PMessage.Create<RawData>(new RawData(data)));

            return true;
        }

        void Connect()
        {

        }

        void Connect(int connectionId)
        {
            UnityEngine.Debug.Log($"{this.GetType().Name}: {MethodBase.GetCurrentMethod().Name}");

            this.connectionStatus[connectionId] = ConnectionStatus.Connecting;

            if (this.connections.ContainsKey(connectionId))
            {

            }
            else
            {

                var connection = new MirrorP2PConnection(signalingURL: signalingURL, signalingKey: signalingKey, roomId: roomId);

                connection.OnConnectedHandler = () => { this.OnConnected(connectionId); };
                connection.OnDisconnectedHandler = () => { this.OnDisconnected(connectionId); };
                connection.OnMessageHandler = (RawData rawData) => { this.OnMessage(connectionId, rawData); };
                connection.OnRequestHandler = (Type type, IRequest request) => { this.OnRequest(connectionId, type, request); };

                connection.Connect();

                this.connections[connectionId] = connection;
            }
        }

        public void DisconnectAll()
        {
            foreach (var connections in this.connections)
            {
                this.Disconnect(connections.Key);
            }
        }

        public bool Disconnect(int connectionId)
        {
            this.connectionStatus[connectionId] = ConnectionStatus.Disconnected;

            if (this.connections[connectionId] != default)
            {
                this.connections[connectionId].OnDisconnectedHandler = default;
                this.connections[connectionId].OnConnectedHandler = default;
                this.connections[connectionId].OnMessageHandler = default;
                this.connections[connectionId].OnRequestHandler = default;
                this.connections[connectionId].Disconnect();
                this.connections.Remove(connectionId);
            }

            return true;
        }

        public bool IsRunning()
        {
            if (this.state != State.Runnning) return false;

            return true;
        }

        public bool IsConnected(int connectionid)
        {
            if (!this.connectionStatus.ContainsKey(connectionid)) return false;
            if (this.connectionStatus[connectionid] != ConnectionStatus.Connected) return false;

            if (!this.connections.ContainsKey(connectionid)) return false;
            if (!this.connections[connectionid].IsConnected()) return false;

            return true;
        }

        void OnMessage(int connectionId, RawData rawData)
        {
            if (this.connectionStatus[connectionId] != ConnectionStatus.Connected) return;
            this.OnReceivedDataAction?.Invoke(connectionId, rawData.rawData, MirrorP2PServer.channelId);
        }

        void OnRequest(int connectionId, Type type, IRequest request)
        {
            if (type == typeof(ConnectedConfirmRequest))
            {
                this.connections[connectionId].SendResponce(MirrorP2PMessage.Create<ConnectedConfirmResponce>(new ConnectedConfirmResponce(request as ConnectedConfirmRequest)));
                UnityEngine.Debug.Log($"Server OnConnected");
                if (this.connectionStatus[connectionId] == ConnectionStatus.Connected) return;
                this.connectionStatus[connectionId] = ConnectionStatus.Connected;
                this.OnConnectedAction?.Invoke(connectionId);
            }
        }

        void OnConnected(int connectionId)
        {

        }

        string GenerateRoomId(int connectionId)
        {
            return $"{this.roomId}_{connectionId}";
        }

        void OnDisconnected(int connectionId)
        {
            this.connectionStatus[connectionId] = ConnectionStatus.Disconnected;
            this.Disconnect(connectionId);

            this.OnDisconnectedAction?.Invoke(connectionId);
            UnityEngine.Debug.Log("MirrorP2PServer:OnDisconnected");

            if (this.state == State.Runnning)
            {
                this.Connect();
            }
            else if (this.state == State.Stop)
            {
                this.connections.Remove(connectionId);
            }
        }
    }
}
