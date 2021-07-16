using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using Unity.WebRTC;
using UnityEngine;

namespace Mirror.WebRTC
{
    public class MirrorP2PConnection
    {
        // TODO: UniRX
        public delegate void OnMessageHandler(string dataChannelLabel, byte[] rawData);
        public delegate void OnConnectedHandler();
        public delegate void OnDisconnectedHandler();

        public event OnMessageHandler onMessage;
        public event OnConnectedHandler onConnected;
        public event OnDisconnectedHandler onDisconnected;

        static readonly float interval = 5.0f;
        static readonly string dataChannelLabel = "Mirror";

        string signalingURL;
        string signalingKey;
        string roomId;

        DateTime lastMessagedTime = default;

        Dictionary<int, UniTaskCompletionSource<bool>> utcss = new Dictionary<int, UniTaskCompletionSource<bool>>();

        public enum State
        {
            Running,
            Stop,
        }

        State state = State.Stop;

        AyameConnection ayameConnection = default;

        public MirrorP2PConnection(string signalingURL, string signalingKey, string roomId)
        {
            this.signalingKey = signalingKey;
            this.signalingURL = signalingURL;
            this.roomId = roomId;
        }

        public void Connect()
        {
            if (this.state == State.Running) return;
            this.state = State.Running;

            this.ayameConnection = new AyameConnection();
            this.ayameConnection.OnConnectedHandler += this.OnConnected;
            var dataChannelLabels = new string[] { dataChannelLabel };
            this.ayameConnection.Connect(this.signalingURL, this.signalingKey, this.roomId, dataChannelLabels, interval);
        }

        public void Disconnect()
        {
            if (this.state == State.Stop) return;
            this.state = State.Stop;
            this.ayameConnection.Disconnect();
            this.ayameConnection = default;
            Debug.Log("Disconnect");
        }

        public bool IsConnectedAllDataChannel()
        {
            return true;
        }

        public bool SendMessage(byte[] message)
        {
            if (!this.IsConnectedAllDataChannel()) return false;

            var mirrorP2PMessage = MirrorP2PMessage.CreateRawDataMessage(message);
            this.ayameConnection.SendMessage(MirrorP2PConnection.dataChannelLabel, mirrorP2PMessage.ToPayload());

            return true;
        }

        async UniTask<bool> SendMessage(string dataChannelLabel, MirrorP2PMessage message, CancellationToken ct)
        {
            if (this.utcss.ContainsKey(message.Uid)) return false;

            CancellationTokenSource timeOutCT = new CancellationTokenSource();
            timeOutCT.CancelAfterSlim(TimeSpan.FromSeconds(3));

            var utcs = new UniTaskCompletionSource<bool>();
            this.utcss[message.Uid] = utcs;
            this.ayameConnection.SendMessage(dataChannelLabel, message.ToPayload());

            bool result = false;

            try
            {
                var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(timeOutCT.Token, ct);
                result = await utcs.Task.WithCancellation(linkedTokenSource.Token);
            }
            finally
            {
                this.utcss.Remove(message.Uid);
            }

            return result;
        }

        void OnMessage(RTCDataChannel dataChannel, byte[] bytes)
        {
            var mirrorP2PMessage = MirrorP2PMessage.LoadMessage(bytes);

            UnityEngine.Debug.Log($"OnMessage: label {dataChannel.Label},uid {mirrorP2PMessage.Uid}, Type {mirrorP2PMessage.MessageType.ToString()}");

            switch (mirrorP2PMessage.MessageType)
            {
                case MirrorP2PMessage.Type.Ping:
                    {
                        var message = MirrorP2PMessage.CreatePongMessage(mirrorP2PMessage.Uid);
                        dataChannel.Send(message.ToPayload());

                        break;
                    }

                case MirrorP2PMessage.Type.ConnectedConfirmResponce:
                case MirrorP2PMessage.Type.Pong:
                    {
                        if (!this.utcss.ContainsKey(mirrorP2PMessage.Uid)) return;
                        this.utcss[mirrorP2PMessage.Uid].TrySetResult(true);
                        break;
                    }

                case MirrorP2PMessage.Type.ConnectedConfirmRequest:
                    {
                        var message = MirrorP2PMessage.CreateConnectedConfirmResponce(mirrorP2PMessage.Uid);
                        dataChannel.Send(message.ToPayload());
                        /*                        this.otherDataChannelConnected = true;
                                                if (this.myDataChannelConencted) this.OnConnectedAllDataChannel();*/
                        break;
                    }

                case MirrorP2PMessage.Type.RawData:
                    {
                        this.onMessage?.Invoke(dataChannel.Label, mirrorP2PMessage.rawData);
                        break;
                    }

                default:
                    break;
            }
        }

        void OnConnected(RTCDataChannel channel)
        {
            /*            UniTask.Void(async () =>
                        {
                            bool result = false;
                            while (!result)
                            {
                                try
                                {
                                    if (this.state != State.Running) break;
                                    var message = MirrorP2PMessage.CreateConnectedConfirmRequest();
                                    var ct = new CancellationToken();
                                    result = await this.SendMessage(channel.Label, message, ct);
                                    if (!result) continue;

                                    this.myDataChannelConencted = true;
                                    if (this.otherDataChannelConnected) this.OnConnectedAllDataChannel();
                                }
                                catch (OperationCanceledException ex)
                                {

                                }
                            }
                        });*/
        }
    }
}
