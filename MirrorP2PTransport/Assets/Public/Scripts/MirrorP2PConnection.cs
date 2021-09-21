using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using UnityEngine;

namespace Mirror.WebRTC
{
    public class MirrorP2PConnection : IDisposable
    {
        public delegate void OnMessageDelegate(RawData rawData);
        public delegate void OnRequestDelegate(Type type, IRequest message);
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

        Dictionary<Guid, UniTaskCompletionSource<IResponse>> utcss = new Dictionary<Guid, UniTaskCompletionSource<IResponse>>();

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

        public void Dispose()
        {
            this.ClearEvents();
            foreach (var utcs in this.utcss) utcs.Value.TrySetCanceled();
            this.ayameConnection?.Dispose();
            this.ayameConnection = default;
        }

        public void ClearEvents()
        {
            this.OnMessageHandler = default;
            this.OnRequestHandler = default;
            this.OnConnectedHandler = default;
            this.OnDisconnectedHandler = default;
        }

        public void Connect()
        {
            UnityEngine.Debug.Log($"{this.GetType().Name}: {MethodBase.GetCurrentMethod().Name}");
            if (this.state == State.Running) return;
            this.state = State.Running;

            this.ayameConnection = new AyameConnection();
            this.ayameConnection.OnConnectedHandler = () => { this.OnConnectedHandler?.Invoke(); };
            this.ayameConnection.OnDisconnectedHandler = () => { this.OnDisconnectedHandler?.Invoke(); };
            this.ayameConnection.OnMessageHandler = this.OnMessage;
            this.ayameConnection.Connect(this.signalingURL, this.signalingKey, this.roomId, interval);
        }

        public void Disconnect()
        {
            if (this.state == State.Stop) return;
            this.state = State.Stop;
            this.ayameConnection.Disconnect();
            this.ayameConnection.ClearEvents();
            this.ayameConnection = default;
            Debug.Log("Disconnect");
        }

        public bool IsConnected()
        {
            if (this.ayameConnection == default) return false;
            return this.ayameConnection.IsConnected();
        }

        public bool SendMessage(MirrorP2PMessage message)
        {
            if (!this.IsConnected()) return false;
            Debug.Log($"SendMessage: {message.Type}");
            this.ayameConnection.SendMessage(message.ToRawData());

            return true;
        }

        public async UniTask<IResponse> SendRequest<T>(T request, CancellationToken ct) where T : IRequest
        {
            if (this.utcss.ContainsKey(request.Uid)) return default;

            CancellationTokenSource timeOutCT = new CancellationTokenSource();
            timeOutCT.CancelAfterSlim(TimeSpan.FromSeconds(3));

            var utcs = new UniTaskCompletionSource<IResponse>();
            this.utcss[request.Uid] = utcs;

            var message = MirrorP2PMessage.Create<T>(request);

            Debug.Log($"SendRequest: {message.Type}");
            this.ayameConnection.SendMessage(message.ToRawData());

            IResponse result = default;

            try
            {
                var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(timeOutCT.Token, ct);
                result = await utcs.Task.AttachExternalCancellation(linkedTokenSource.Token);
            }
            catch (OperationCanceledException ex)
            {
                if (timeOutCT.IsCancellationRequested)
                {
                    return default;
                }

                throw ex;
            }
            finally
            {
                this.utcss.Remove(request.Uid);
            }

            return result;
        }

        public void SendResponce(MirrorP2PMessage message)
        {
            this.SendMessage(message);
        }

        void OnMessage(byte[] bytes)
        {
            var mirrorP2PMessage = MirrorP2PMessage.Create(bytes);

            Debug.Log($"OnMessage: {mirrorP2PMessage.Type}");

            if (mirrorP2PMessage.Type == typeof(RawData))
            {
                this.OnMessageHandler?.Invoke(MirrorP2PMessage.Deserialize(mirrorP2PMessage.Payload) as RawData);
            }
            else if (mirrorP2PMessage.Type.GetInterfaces().Contains(typeof(IRequest)))
            {
                this.OnRequestHandler?.Invoke(mirrorP2PMessage.Type, MirrorP2PMessage.Deserialize(mirrorP2PMessage.Payload) as IRequest);
            }
            else if (mirrorP2PMessage.Type.GetInterfaces().Contains(typeof(IResponse)))
            {
                var responce = MirrorP2PMessage.Deserialize(mirrorP2PMessage.Payload) as IResponse;
                if (!this.utcss.ContainsKey(responce.Uid)) return;
                this.utcss[responce.Uid].TrySetResult(responce);
            }
        }
    }
}
