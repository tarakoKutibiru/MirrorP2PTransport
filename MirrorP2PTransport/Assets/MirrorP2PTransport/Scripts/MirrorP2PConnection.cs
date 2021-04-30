using Ayame.Signaling;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using Unity.WebRTC;

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

        AyameSignaling signaling = default;
        RTCConfiguration rtcConfiguration = default;

        RTCPeerConnection peerConnection = default;
        RTCDataChannel myDataChannel = default;
        RTCDataChannel otherDataChannel = default;
        bool myDataChannelConencted = false;
        bool otherDataChannelConnected = false;
        DateTime lastMessagedTime = default;

        Dictionary<int, UniTaskCompletionSource<bool>> utcss = new Dictionary<int, UniTaskCompletionSource<bool>>();

        public enum State
        {
            Running,
            Stop,
        }

        State state = State.Stop;

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

            this.signaling = new AyameSignaling(this.signalingURL, this.signalingKey, this.roomId, MirrorP2PConnection.interval);
            this.signaling.OnAccept += OnAccept;
            this.signaling.OnAnswer += OnAnswer;
            this.signaling.OnOffer += OnOffer;
            this.signaling.OnIceCandidate += OnIceCandidate;

            this.rtcConfiguration = new RTCConfiguration();
            this.rtcConfiguration.iceServers = new[] { new RTCIceServer { urls = new[] { "stun:stun.l.google.com:19302" } } };

            this.signaling.Start();
        }

        public void Disconnect()
        {
            if (this.state == State.Stop) return;
            this.state = State.Stop;

            this.signaling?.Stop();
            this.signaling = default;
            this.rtcConfiguration = default;

            this.peerConnection = default;
            this.myDataChannel = default;
            this.otherDataChannel = default;
        }

        public bool IsConnectedAllDataChannel()
        {
            if (this.myDataChannel == default) return false;
            if (!this.myDataChannelConencted) return false;

            if (this.otherDataChannel == default) return false;
            if (!this.otherDataChannelConnected) return false;

            return true;
        }

        public bool SendMessage(byte[] message)
        {
            if (!this.IsConnectedAllDataChannel()) return false;

            var mirrorP2PMessage = MirrorP2PMessage.CreateRawDataMessage(message);
            this.myDataChannel.Send(mirrorP2PMessage.ToPayload());

            return true;
        }

        async UniTask<bool> SendMessage(MirrorP2PMessage message, RTCDataChannel dataChannel, CancellationToken ct)
        {
            if (this.utcss.ContainsKey(message.Uid)) return false;

            CancellationTokenSource timeOutCT = new CancellationTokenSource();
            timeOutCT.CancelAfterSlim(TimeSpan.FromSeconds(3));

            var utcs = new UniTaskCompletionSource<bool>();
            this.utcss[message.Uid] = utcs;
            dataChannel.Send(message.ToPayload());

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
                        this.otherDataChannelConnected = true;
                        if (this.myDataChannelConencted) this.OnConnectedAllDataChannel();
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

        void OnConnectedAllDataChannel()
        {
            UnityEngine.Debug.Log("OnConnected");
            this.onConnected?.Invoke();
        }

        void OnDisconnectedDataChannel(RTCDataChannel dataChannel)
        {
            UnityEngine.Debug.LogError("OnDisconnected");

            if (this.IsConnectedAllDataChannel()) return;
            this.OnDisconnected();
        }

        void OnDisconnected()
        {
            if (this.state != State.Running) return;
            this.Disconnect();
            this.onDisconnected?.Invoke();
        }

        #region signaling
        void OnAccept(AyameSignaling signaling)
        {
            AcceptMessage acceptMessage = signaling.m_acceptMessage;

            this.rtcConfiguration.iceServers = acceptMessage.ToRTCIceServers(this.rtcConfiguration.iceServers);

            bool shouldSendOffer = acceptMessage.isExistClient;

            // 相手からのOfferを待機する
            if (!shouldSendOffer) return;

            this.SendOffer(acceptMessage.connectionId, this.rtcConfiguration).Forget();
        }

        async UniTask<bool> SendOffer(string connectionId, RTCConfiguration rtcConfiguration)
        {
            var pc = this.CreatePeerConnection(connectionId, rtcConfiguration);
            this.peerConnection = pc;

            var options = new RTCOfferAnswerOptions();
            options.iceRestart = false;
            options.voiceActivityDetection = false;

            var offer = pc.CreateOffer(ref options);
            await offer;
            if (offer.IsError) return false;

            var desc = offer.Desc;
            var localDescription = pc.SetLocalDescription(ref desc);
            await localDescription;
            if (localDescription.IsError) return false;

            this.signaling.SendOffer(connectionId, pc.LocalDescription);

            return true;
        }

        void OnOffer(ISignaling signaling, DescData descData)
        {
            RTCSessionDescription description = new RTCSessionDescription();
            description.type = RTCSdpType.Offer;
            description.sdp = descData.sdp;

            this.SendAnswer(descData.connectionId, this.rtcConfiguration, description).Forget();
        }

        async UniTask<bool> SendAnswer(string connectionId, RTCConfiguration rtcConfiguration, RTCSessionDescription rtcSessionDescription)
        {
            var pc = this.CreatePeerConnection(connectionId, rtcConfiguration);
            this.peerConnection = pc;

            var remoteDescription = pc.SetRemoteDescription(ref rtcSessionDescription);
            await remoteDescription;
            if (remoteDescription.IsError) return false;

            var options = new RTCOfferAnswerOptions();
            options.iceRestart = false;
            options.voiceActivityDetection = false;
            var answer = pc.CreateAnswer(ref options);
            await answer;

            var desc = answer.Desc;
            var localDescription = pc.SetLocalDescription(ref desc);
            await localDescription;
            if (localDescription.IsError) return false;

            this.signaling.SendAnswer(connectionId, pc.LocalDescription);

            return true;
        }

        void OnAnswer(ISignaling signaling, DescData descData)
        {
            RTCSessionDescription description = new RTCSessionDescription();
            description.type = RTCSdpType.Answer;
            description.sdp = descData.sdp;

            this.peerConnection.SetRemoteDescription(ref description);
        }

        void OnIceCandidate(ISignaling signaling, CandidateData candidateData)
        {
            RTCIceCandidateInit option = new RTCIceCandidateInit();
            option.candidate = candidateData.candidate;
            option.sdpMid = candidateData.sdpMid;
            option.sdpMLineIndex = candidateData.sdpMLineIndex;

            RTCIceCandidate iceCandidate = new RTCIceCandidate(option);

            UnityEngine.Debug.Log($"OnIceCandidate: candidate {option.candidate}");

            this.peerConnection.AddIceCandidate(iceCandidate);
        }

        RTCPeerConnection CreatePeerConnection(string connectionId, RTCConfiguration rtcConfiguration)
        {
            var pc = new RTCPeerConnection(ref rtcConfiguration);

            RTCDataChannelInit dataChannelInit = new RTCDataChannelInit();
            dataChannelInit.ordered = true;

            RTCDataChannel dataChannel = pc.CreateDataChannel(MirrorP2PConnection.dataChannelLabel, dataChannelInit);
            dataChannel.OnOpen += () => this.OnOpenChannel(this.signaling.ClientId, dataChannel);
            dataChannel.OnMessage += bytes => this.OnMessage(dataChannel, bytes);
            dataChannel.OnClose += () =>
            {
                this.myDataChannel = default;
                this.myDataChannelConencted = false;

                this.OnDisconnectedDataChannel(dataChannel);
            };

            pc.OnDataChannel = channel => this.OnDataChannel(connectionId, channel);
            pc.OnIceCandidate = candidate =>
            {
                this.signaling?.SendCandidate(connectionId, candidate);
            };

            pc.OnIceConnectionChange = state =>
            {
                if (state != RTCIceConnectionState.Disconnected) return;
                pc.Close();
                this.peerConnection = default;
                this.OnDisconnected();
            };

            return pc;
        }

        #region DataChannel

        /// <summary>
        /// 他方のピアで作成されたDataChannelが接続されたときに呼ばれる。
        /// </summary>
        /// <param name="pc"></param>
        /// <param name="channel"></param>
        void OnDataChannel(string connectionId, RTCDataChannel dataChannel)
        {
            UnityEngine.Debug.Log($"OnDataChannel: {connectionId}");

            this.otherDataChannel = dataChannel;
            dataChannel.OnMessage += bytes => this.OnMessage(dataChannel, bytes);
            dataChannel.OnClose += () =>
            {
                this.otherDataChannel = default;
                this.otherDataChannelConnected = false;

                this.OnDisconnectedDataChannel(dataChannel);
            };
        }

        void OnOpenChannel(string connectionId, RTCDataChannel channel)
        {
            UnityEngine.Debug.Log($"OnOpenChannnel: {connectionId}");
            this.myDataChannel = channel;

            UniTask.Void(async () =>
            {
                bool result = false;
                while (!result)
                {
                    try
                    {
                        if (this.state != State.Running) break;
                        var message = MirrorP2PMessage.CreateConnectedConfirmRequest();
                        var ct = new CancellationToken();
                        result = await this.SendMessage(message, channel, ct);
                        if (!result) continue;

                        this.myDataChannelConencted = true;
                        if (this.otherDataChannelConnected) this.OnConnectedAllDataChannel();
                    }
                    catch (OperationCanceledException ex)
                    {

                    }
                }
            });
        }
    }

    #endregion

    #endregion
}
