using Ayame.Signaling;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using Unity.WebRTC;
using UnityEngine;

namespace Mirror.WebRTC
{
    using DataChannelDictionary = Dictionary<int, RTCDataChannel>;

    public class Common
    {
        protected string signalingURL = "";
        protected string signalingKey = "";
        protected string roomId = "";

        [SerializeField, Tooltip("Time interval for polling from signaling server")]
        private float interval = 5.0f;

        private AyameSignaling signaling = null;
        private readonly Dictionary<string, RTCPeerConnection> peerConnections = new Dictionary<string, RTCPeerConnection>();
        private readonly Dictionary<RTCPeerConnection, DataChannelDictionary> m_mapPeerAndChannelDictionary = new Dictionary<RTCPeerConnection, DataChannelDictionary>();
        private RTCConfiguration rtcConfiguration;

        public void Start()
        {
            if (this.signaling != default) return;

            this.rtcConfiguration = new RTCConfiguration();
            this.signaling = new AyameSignaling(signalingURL, signalingKey, roomId, interval);

            this.signaling.OnAccept += OnAccept;
            this.signaling.OnAnswer += OnAnswer;
            this.signaling.OnOffer += OnOffer;
            this.signaling.OnIceCandidate += OnIceCandidate;
            this.signaling.OnBye += OnBye;
            this.signaling.OnWSDisconected += OnWSDisconnected;
            this.signaling.Start();
        }

        public virtual void Stop()
        {
            if (this.signaling == default) return;

            this.signaling.Stop();
            this.signaling = null;

            this.peerConnections.Clear();
            this.m_mapPeerAndChannelDictionary.Clear();
            this.rtcConfiguration = default;

            this.OnDisconnected();
        }

        void OnAccept(AyameSignaling ayameSignaling)
        {
            AcceptMessage acceptMessage = ayameSignaling.m_acceptMessage;

            bool shouldSendOffer = acceptMessage.isExistClient;

            this.rtcConfiguration.iceServers = acceptMessage.ToRTCIceServers();

            // 相手からのOfferを待つ
            if (!shouldSendOffer) return;

            this.SendOffer(acceptMessage.connectionId, this.rtcConfiguration).Forget();
        }

        async UniTask<bool> SendOffer(string connectionId, RTCConfiguration configuration)
        {
            var pc = new RTCPeerConnection(ref configuration);
            this.peerConnections.Add(connectionId, pc);

            // create data chennel
            RTCDataChannelInit dataChannelOptions = new RTCDataChannelInit();

            RTCDataChannel dataChannel = pc.CreateDataChannel("dataChannel", dataChannelOptions);
            dataChannel.OnMessage = bytes => OnMessage(dataChannel, bytes);
            dataChannel.OnOpen = () => OnOpenChannel(connectionId, dataChannel);
            dataChannel.OnClose = () => OnCloseChannel(connectionId, dataChannel);

            pc.OnDataChannel = new DelegateOnDataChannel(channel => { OnDataChannel(pc, channel); });
            pc.OnIceCandidate = new DelegateOnIceCandidate(candidate =>
            {
                this.signaling.SendCandidate(connectionId, candidate);
            });

            pc.OnIceConnectionChange = new DelegateOnIceConnectionChange(state =>
            {
                if (state == RTCIceConnectionState.Disconnected)
                {
                    pc.Close();
                    this.peerConnections.Remove(connectionId);

                    this.Stop();
                }
            });

            RTCOfferOptions options = new RTCOfferOptions();
            options.iceRestart = false;
            options.offerToReceiveAudio = false;
            options.offerToReceiveVideo = false;

            var offer = pc.CreateOffer(ref options);
            await offer;
            if (offer.IsError) return false;

            var desc = offer.Desc;
            var localDescriptionOperation = pc.SetLocalDescription(ref desc);

            this.signaling.SendOffer(connectionId, pc.LocalDescription);

            return true;
        }

        void OnOffer(ISignaling signaling, DescData e)
        {
            if (this.peerConnections.ContainsKey(e.connectionId)) return;

            RTCSessionDescription sessionDescriotion;
            sessionDescriotion.type = RTCSdpType.Offer;
            sessionDescriotion.sdp = e.sdp;

            this.SendAnswer(e.connectionId, sessionDescriotion).Forget();
        }

        async UniTask<bool> SendAnswer(string connectionId, RTCSessionDescription sessionDescriotion)
        {
            var pc = new RTCPeerConnection(ref this.rtcConfiguration);
            this.peerConnections.Add(connectionId, pc);

            pc.OnDataChannel = new DelegateOnDataChannel(channel => { OnDataChannel(pc, channel); });
            pc.OnIceCandidate = new DelegateOnIceCandidate(candidate =>
            {
                this.signaling.SendCandidate(connectionId, candidate);
            });

            pc.OnIceConnectionChange = new DelegateOnIceConnectionChange(state =>
            {
                if (state == RTCIceConnectionState.Disconnected)
                {
                    pc.Close();
                    this.peerConnections.Remove(connectionId);

                    this.Stop();
                }
            });

            var remoteDescriptionOperation = pc.SetRemoteDescription(ref sessionDescriotion);
            await remoteDescriptionOperation;
            if (remoteDescriptionOperation.IsError) return false;

            RTCAnswerOptions options = default;

            var answer = pc.CreateAnswer(ref options);
            await answer;

            var desc = answer.Desc;
            var localDescriptionOperation = pc.SetLocalDescription(ref desc);
            await localDescriptionOperation;
            if (localDescriptionOperation.IsError) return false;

            this.signaling.SendAnswer(connectionId, desc);

            return true;
        }

        void OnAnswer(ISignaling signaling, DescData e)
        {
            RTCSessionDescription desc = new RTCSessionDescription();
            desc.type = RTCSdpType.Answer;
            desc.sdp = e.sdp;

            RTCPeerConnection pc = this.peerConnections[e.connectionId];
            pc.SetRemoteDescription(ref desc);
        }

        protected void SendBye()
        {
            this.signaling?.SendBye();
        }

        void OnBye()
        {
            this.Stop();
        }

        void OnWSDisconnected()
        {
            this.Stop();
        }

        void OnIceCandidate(ISignaling signaling, CandidateData e)
        {
            if (!this.peerConnections.TryGetValue(e.connectionId, out var pc)) return;


            RTCIceCandidateInit rtcIceCandidateInit = new RTCIceCandidateInit();
            rtcIceCandidateInit.candidate = e.candidate;
            rtcIceCandidateInit.sdpMLineIndex = e.sdpMLineIndex;
            rtcIceCandidateInit.sdpMid = e.sdpMid;
            RTCIceCandidate​ iceCandidate = new RTCIceCandidate(rtcIceCandidateInit);

            pc.AddIceCandidate(iceCandidate);
        }

        /// <summary>
        /// 他方のピアで作成されたDataChannelが接続されたときに呼ばれる。
        /// </summary>
        /// <param name="pc"></param>
        /// <param name="channel"></param>
        void OnDataChannel(RTCPeerConnection pc, RTCDataChannel channel)
        {
            if (!m_mapPeerAndChannelDictionary.TryGetValue(pc, out var channels))
            {
                channels = new DataChannelDictionary();
                m_mapPeerAndChannelDictionary.Add(pc, channels);
            }
            channels.Add(channel.Id, channel);

            channel.OnMessage = bytes => OnMessage(channel, bytes);
            channel.OnClose = () => OnCloseChannel(this.signaling?.m_acceptMessage?.connectionId, channel);

            this.OnConnected();
        }

        /// <summary>
        /// 自分のピアで作成したDataChannelの接続が確立されたときに呼ばれる。
        /// </summary>
        /// <param name="channel"></param>
        void OnOpenChannel(string connectionId, RTCDataChannel channel)
        {
            var pc = this.peerConnections[connectionId];

            if (!m_mapPeerAndChannelDictionary.TryGetValue(pc, out var channels))
            {
                channels = new DataChannelDictionary();
                m_mapPeerAndChannelDictionary.Add(pc, channels);
            }
            channels.Add(channel.Id, channel);

            channel.OnMessage = bytes => OnMessage(channel, bytes);
            channel.OnClose = () => OnCloseChannel(connectionId, channel);

            this.OnConnected();
        }

        /// <summary>
        /// 自分のピアで作成したDataChannelの接続が切れたとき
        /// </summary>
        /// <param name="channel"></param>
        void OnCloseChannel(string connectionId, RTCDataChannel channel)
        {
            this.Stop();
        }

        protected virtual void OnMessage(RTCDataChannel channel, byte[] bytes)
        {
            string text = System.Text.Encoding.UTF8.GetString(bytes);
            this.OnMessage(text);
        }

        protected virtual void OnMessage(string message)
        {
            Debug.LogFormat("OnMessage {0}", message);
        }

        public void SendMessage(string message)
        {
            RTCDataChannel dataChannel = this.GetDataChannel("dataChannel");
            if (dataChannel == null) return;

            if (dataChannel.ReadyState != RTCDataChannelState.Open)
            {
                Debug.LogError("Not Open.");
                return;
            }

            dataChannel.Send(message);

            // Debug.Log("Send Message");
        }

        protected bool SendMessage(byte[] bytes)
        {
            RTCDataChannel dataChannel = this.GetDataChannel("dataChannel");
            if (dataChannel == null) return false;

            if (dataChannel.ReadyState != RTCDataChannelState.Open)
            {
                Debug.LogError("Not Open.");
                return false;
            }

            dataChannel.Send(bytes);

            // Debug.Log("Send Message");

            return true;
        }

        protected virtual void OnConnected()
        {
            Debug.Log("OnConnected");
        }

        protected virtual void OnDisconnected()
        {
            Debug.Log("OnDisconnected");
        }

        protected bool IsConnected()
        {
            if (this.signaling == default) return false;

            RTCDataChannel dataChannel = this.GetDataChannel("dataChannel");
            if (dataChannel == null) return false;
            return dataChannel.ReadyState == RTCDataChannelState.Open;
        }

        RTCDataChannel GetDataChannel(string label)
        {
            foreach (var dictionary in m_mapPeerAndChannelDictionary.Values)
            {
                foreach (RTCDataChannel dataChannel in dictionary.Values)
                {
                    if (dataChannel.Label == label) return dataChannel;
                }
            }

            return null;
        }
    }
}
