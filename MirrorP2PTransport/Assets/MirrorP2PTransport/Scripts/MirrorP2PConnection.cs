using Ayame.Signaling;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using Unity.WebRTC;

namespace Mirror.WebRTC
{
    using DataChannelDictionary = Dictionary<int, RTCDataChannel>;

    public class MirrorP2PConnection
    {
        static readonly string dataChannelLabel = "dataChannel";

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

        private readonly Dictionary<string, RTCPeerConnection> peerConnections = new Dictionary<string, RTCPeerConnection>();
        private readonly Dictionary<RTCPeerConnection, DataChannelDictionary> mapPeerAndChannelDictionary = new Dictionary<RTCPeerConnection, DataChannelDictionary>();

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

            this.signaling.Start();
        }

        public void Disconnect()
        {
            if (this.state == State.Stop) return;

            this.signaling?.Stop();
            this.signaling = default;
            this.rtcConfiguration = default;

            this.peerConnections.Clear();
            this.mapPeerAndChannelDictionary.Clear();
        }

        public bool IsConnected()
        {
            var dataChannel = this.GetDataChannel(MirrorP2PConnection.dataChannelLabel);
            if (dataChannel == default) return false;

            if (dataChannel.ReadyState != RTCDataChannelState.Open) return false;

            return true;
        }

        public bool SendMessage(byte[] bytes)
        {
            //   UnityEngine.Debug.Log("SendMessage");

            if (!this.IsConnected())
            {
                UnityEngine.Debug.LogError("SendMessage Error. Is not connected.");

                return false;
            }

            this.GetDataChannel(MirrorP2PConnection.dataChannelLabel).Send(bytes);

            return true;
        }

        void OnMessage(RTCDataChannel dataChannel, byte[] bytes)
        {
            UnityEngine.Debug.Log($"OnMessage: label {dataChannel.Label}");

            this.onMessage?.Invoke(bytes);
        }

        void OnConnected()
        {
            UnityEngine.Debug.LogError("OnConnected");
            this.onConnected?.Invoke();
        }

        void OnDisconnected()
        {
            UnityEngine.Debug.LogError("OnDisconnected");

            if (this.state == State.Running)
            {
                this.Disconnect();
                this.onDisconnected?.Invoke();
            }
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
            this.peerConnections.Add(connectionId, pc);

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
            this.peerConnections.Add(connectionId, pc);

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

            this.peerConnections[descData.connectionId].SetRemoteDescription(ref description);
        }

        void OnIceCandidate(ISignaling signaling, CandidateData candidateData)
        {
            RTCIceCandidateInit option = new RTCIceCandidateInit();
            option.candidate = candidateData.candidate;
            option.sdpMid = candidateData.sdpMid;
            option.sdpMLineIndex = candidateData.sdpMLineIndex;

            RTCIceCandidate iceCandidate = new RTCIceCandidate(option);

            UnityEngine.Debug.Log($"OnIceCandidate: candidate {option.candidate}");

            this.peerConnections[candidateData.connectionId].AddIceCandidate(iceCandidate);
        }

        RTCPeerConnection CreatePeerConnection(string connectionId, RTCConfiguration rtcConfiguration)
        {
            var pc = new RTCPeerConnection(ref this.rtcConfiguration);

            RTCDataChannelInit dataChannelInit = new RTCDataChannelInit();
            dataChannelInit.ordered = true;

            RTCDataChannel dataChannel = pc.CreateDataChannel(MirrorP2PConnection.dataChannelLabel, dataChannelInit);
            dataChannel.OnOpen += () => this.OnOpenChannel(connectionId, dataChannel);
            dataChannel.OnMessage += bytes => this.OnMessage(dataChannel, bytes);
            dataChannel.OnClose += () => this.OnCloseChannel(connectionId, dataChannel);

            pc.OnDataChannel = channel => this.OnDataChannel(connectionId, channel);
            pc.OnIceCandidate = candidate =>
            {
                this.signaling?.SendCandidate(connectionId, candidate);
            };
            pc.OnIceConnectionChange = state =>
            {
                if (state != RTCIceConnectionState.Disconnected) return;
                pc.Close();
                this.peerConnections.Remove(connectionId);
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

            dataChannel.OnOpen += () => this.OnOpenChannel(connectionId, dataChannel);
            dataChannel.OnMessage += bytes => this.OnMessage(dataChannel, bytes);
            dataChannel.OnClose += () => this.OnCloseChannel(connectionId, dataChannel);

            this.AddDataChannel(connectionId, dataChannel);
        }

        void OnOpenChannel(string connectionId, RTCDataChannel channel)
        {
            UnityEngine.Debug.Log($"OnOpenChannnel: {connectionId}");

            this.AddDataChannel(connectionId, channel);

            this.OnConnected();
        }

        void OnCloseChannel(string connectionId, RTCDataChannel channel)
        {
            UnityEngine.Debug.Log($"OnCloseChannel: {connectionId}");

            this.OnDisconnected();
        }

        RTCDataChannel GetDataChannel(string label)
        {
            foreach (var dictionary in this.mapPeerAndChannelDictionary.Values)
            {
                foreach (RTCDataChannel dataChannel in dictionary.Values)
                {
                    if (dataChannel.Label == label) return dataChannel;
                }
            }

            return null;
        }

        void AddDataChannel(string connectionId, RTCDataChannel dataChannnel)
        {
            try
            {
                var pc = this.peerConnections[connectionId];

                if (!this.mapPeerAndChannelDictionary.TryGetValue(pc, out var channels))
                {
                    channels = new DataChannelDictionary();
                    this.mapPeerAndChannelDictionary.Add(pc, channels);
                }
                channels.Add(dataChannnel.Id, dataChannnel);
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError("AddDataChannel: " + ex);
            }
        }
    }

    #endregion

    #endregion
}
