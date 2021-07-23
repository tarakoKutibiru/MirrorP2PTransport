using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using UnityEngine;

namespace Mirror.WebRTC
{
    public class MirrorP2PConnection
    {
        public delegate void OnMessageDelegate(MirrorP2PMessage message);
        public delegate void OnRequestDelegate(MirrorP2PMessage message);
        public delegate void OnConnectedDelegate();
        public delegate void OnDisconnectedDelegate();

        public OnMessageDelegate OnMessageHandler;
        public OnRequestDelegate OnRequestHandler;
        public OnConnectedDelegate OnConnectedHandler;
        public OnDisconnectedDelegate OnDisconnectedHandler;

        static readonly float interval = 5.0f;

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
            UnityEngine.Debug.Log($"{this.GetType().Name}: {MethodBase.GetCurrentMethod().Name}");
            if (this.state == State.Running) return;
            this.state = State.Running;

            this.ayameConnection = new AyameConnection();
            this.ayameConnection.OnConnectedHandler += () => { this.OnConnectedHandler?.Invoke(); };
            this.ayameConnection.OnDisconnectedHandler += () => { this.OnDisconnectedHandler?.Invoke(); };
            this.ayameConnection.OnMessageHandler += this.OnMessage;
            this.ayameConnection.Connect(this.signalingURL, this.signalingKey, this.roomId, interval);
        }

        public void Disconnect()
        {
            if (this.state == State.Stop) return;
            this.state = State.Stop;
            this.ayameConnection.Disconnect();
            this.ayameConnection = default;
            Debug.Log("Disconnect");
        }

        public bool IsConnected()
        {
            return this.ayameConnection.IsConnected();
        }

        public bool SendMessage(MirrorP2PMessage message)
        {
            if (!this.IsConnected()) return false;
            Debug.Log($"SendMessage: {message.MessageType}");
            this.ayameConnection.SendMessage(message.ToPayload());

            return true;
        }

        public async UniTask<bool> SendRequest(MirrorP2PMessage message, CancellationToken ct)
        {
            if (this.utcss.ContainsKey(message.Uid)) return false;

            CancellationTokenSource timeOutCT = new CancellationTokenSource();
            timeOutCT.CancelAfterSlim(TimeSpan.FromSeconds(3));

            var utcs = new UniTaskCompletionSource<bool>();
            this.utcss[message.Uid] = utcs;

            Debug.Log($"SendRequest: {message.MessageType}");
            this.ayameConnection.SendMessage(message.ToPayload());

            bool result = false;

            try
            {
                var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(timeOutCT.Token, ct);
                result = await utcs.Task.WithCancellation(linkedTokenSource.Token);
            }
            catch (OperationCanceledException ex)
            {
                if (timeOutCT.IsCancellationRequested)
                {
                    return false;
                }

                throw ex;
            }
            finally
            {
                this.utcss.Remove(message.Uid);
            }

            return result;
        }

        public void SendResponce(MirrorP2PMessage message)
        {
            this.SendMessage(message);
        }

        void OnMessage(byte[] bytes)
        {
            var mirrorP2PMessage = MirrorP2PMessage.LoadMessage(bytes);

            Debug.Log($"OnMessage: {mirrorP2PMessage.MessageType}");

            switch (mirrorP2PMessage.MessageType)
            {
                case MirrorP2PMessage.Type.ConnectedConfirmResponce:
                    {
                        if (!this.utcss.ContainsKey(mirrorP2PMessage.Uid)) return;
                        this.utcss[mirrorP2PMessage.Uid].TrySetResult(true);
                        break;
                    }

                case MirrorP2PMessage.Type.ConnectedConfirmRequest:
                    {
                        this.OnRequestHandler?.Invoke(mirrorP2PMessage);
                        break;
                    }

                case MirrorP2PMessage.Type.RawData:
                    {
                        this.OnMessageHandler?.Invoke(mirrorP2PMessage);
                        break;
                    }

                default:
                    break;
            }
        }
    }
}
