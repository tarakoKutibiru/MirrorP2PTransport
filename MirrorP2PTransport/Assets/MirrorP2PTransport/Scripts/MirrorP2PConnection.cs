using Ayame.Signaling;
using Cysharp.Threading.Tasks;
using Unity.WebRTC;

namespace Mirror.WebRTC
{
    public class MirrorP2PConnection
    {
        // TODO: UniRX
        public delegate void OnMessageHandler(byte[] bytes);
        public delegate void OnConnectedHandler();
        public delegate void OnDisconnectedHandler();

        public event OnMessageHandler onMessage;
        public event OnConnectedHandler onConnected;
        public event OnDisconnectedHandler onDisconnected;

        private static readonly float interval = 5.0f;

        string signalingURL;
        string signalingKey;
        string roomId;

        AyameSignaling signaling = default;
        RTCConfiguration rtcConfiguration = default;
        RTCPeerConnection rtcPeerConnection = default;
        RTCDataChannel rtcDataChannel = default;

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

            this.signaling.Start();
        }

        public void Disconnect()
        {
            if (this.state == State.Stop) return;

            this.signaling.Stop();
            this.signaling = default;
            this.rtcConfiguration = default;
            this.rtcPeerConnection = default;
            this.rtcDataChannel = default;
        }

        public bool IsConnected()
        {
            if (this.rtcDataChannel == default) return false;
            if (this.rtcDataChannel.ReadyState != RTCDataChannelState.Open) return false;

            return false;
        }

        public bool SendMessage(byte[] bytes)
        {
            if (!this.IsConnected()) return false;

            this.rtcDataChannel.Send(bytes);

            return true;
        }

        void OnMessage(RTCDataChannel dataChannel, byte[] bytes)
        {
            this.onMessage?.Invoke(bytes);
        }

        void OnConnected()
        {
            this.onConnected?.Invoke();
        }

        void OnDisconnected()
        {
            this.onDisconnected?.Invoke();
        }

        #region signaling
        void OnAccept(AyameSignaling signaling)
        {
            AcceptMessage acceptMessage = signaling.m_acceptMessage;
            this.rtcConfiguration.iceServers = acceptMessage.ToRTCIceServers();

            bool shouldSendOffer = acceptMessage.isExistClient;

            // 相手からのOfferを待機する
            if (!shouldSendOffer) return;

            this.SendOffer(acceptMessage.connectionId, this.rtcConfiguration).Forget();
        }

        async UniTask<bool> SendOffer(string connectionId, RTCConfiguration rtcConfiguration)
        {
            var pc = this.CreatePeerConnection(connectionId, rtcConfiguration);

            RTCOfferOptions options = new RTCOfferOptions();
            options.iceRestart = false;
            options.offerToReceiveAudio = false;
            options.offerToReceiveVideo = false;

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
            this.rtcPeerConnection = pc;

            var remoteDescription = pc.SetRemoteDescription(ref rtcSessionDescription);
            await remoteDescription;
            if (remoteDescription.IsError) return false;

            RTCAnswerOptions options = new RTCAnswerOptions();
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

            this.rtcPeerConnection?.SetRemoteDescription(ref description);
        }

        void OnIceCandidate(ISignaling signaling, CandidateData candidateData)
        {
            RTCIceCandidateInit option = new RTCIceCandidateInit();
            option.candidate = candidateData.candidate;
            option.sdpMid = candidateData.sdpMid;
            option.sdpMLineIndex = candidateData.sdpMLineIndex;

            RTCIceCandidate iceCandidate = new RTCIceCandidate(option);

            this.rtcPeerConnection?.AddIceCandidate(iceCandidate);
        }

        #region DataChannel

        /// <summary>
        /// 他方のピアで作成されたDataChannelが接続されたときに呼ばれる。
        /// </summary>
        /// <param name="pc"></param>
        /// <param name="channel"></param>
        void OnDataChannel(string connectionId, RTCDataChannel channel)
        {
            if (this.rtcDataChannel != default) return;

            channel.OnOpen += () => this.OnOpenChannel(connectionId, channel);

            if (channel.ReadyState == RTCDataChannelState.Open) this.OnOpenChannel(connectionId, channel);
        }

        void OnOpenChannel(string connectionId, RTCDataChannel channel)
        {
            if (this.rtcDataChannel != default) return;

            channel.OnMessage += bytes => this.OnMessage(channel, bytes);
            channel.OnClose += () => this.OnCloseChannel(connectionId, channel);

            this.rtcDataChannel = channel;

            this.OnConnected();
        }

        void OnCloseChannel(string connectionId, RTCDataChannel channel)
        {
            if (this.rtcDataChannel == default) return;

            this.rtcDataChannel = default;

            this.OnDisconnected();
        }

        RTCPeerConnection CreatePeerConnection(string connectionId, RTCConfiguration rtcConfiguration)
        {
            var pc = new RTCPeerConnection(ref rtcConfiguration);

            RTCDataChannelInit dataChannelInit = new RTCDataChannelInit();
            dataChannelInit.ordered = true;

            RTCDataChannel dataChannel = pc.CreateDataChannel("dataChannel", dataChannelInit);
            dataChannel.OnOpen += () => this.OnOpenChannel(connectionId, dataChannel);

            pc.OnDataChannel = channel => this.OnDataChannel(connectionId, channel);
            pc.OnIceCandidate = candidate =>
            {
                this.signaling?.SendCandidate(connectionId, candidate);
            };
            pc.OnIceConnectionChange = state =>
            {
                if (state != RTCIceConnectionState.Disconnected) return;
                this.OnDisconnected();
            };

            return pc;
        }

        #endregion

        #endregion
    }
}
