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

        [SerializeField, Tooltip("Array to set your own STUN/TURN servers")]
        private RTCIceServer[] iceServers = new RTCIceServer[]
        {
            new RTCIceServer()
            {
                urls = new string[] { "stun:stun.l.google.com:19302" }
            }
        };

        [SerializeField, Tooltip("Time interval for polling from signaling server")]
        private float interval = 5.0f;

        private AyameSignaling m_signaling;
        private readonly Dictionary<string, RTCPeerConnection> m_mapConnectionIdAndPeer = new Dictionary<string, RTCPeerConnection>();
        private readonly Dictionary<RTCPeerConnection, DataChannelDictionary> m_mapPeerAndChannelDictionary = new Dictionary<RTCPeerConnection, DataChannelDictionary>();
        private RTCConfiguration m_conf;


        public void Start()
        {
            m_conf = default;
            m_conf.iceServers = iceServers;

            if (this.m_signaling == null)
            {
                this.m_signaling = new AyameSignaling(signalingURL, signalingKey, roomId, interval);

                this.m_signaling.OnAccept += OnAccept;
                this.m_signaling.OnAnswer += OnAnswer;
                this.m_signaling.OnOffer += OnOffer;
                this.m_signaling.OnIceCandidate += OnIceCandidate;
            }
            this.m_signaling.Start();
        }

        public virtual void Stop()
        {
            if (this.m_signaling != null)
            {
                this.m_signaling.Stop();
                this.m_signaling = null;

                this.m_mapConnectionIdAndPeer.Clear();
                this.m_mapPeerAndChannelDictionary.Clear();
                this.m_conf = default;
            }
        }

        void OnAccept(AyameSignaling ayameSignaling)
        {
            AcceptMessage acceptMessage = ayameSignaling.m_acceptMessage;

            bool shouldSendOffer = acceptMessage.isExistClient;

            var configuration = GetSelectedSdpSemantics();

            this.iceServers = configuration.iceServers;
            this.m_conf.iceServers = this.iceServers;

            // 相手からのOfferを待つ
            if (!shouldSendOffer) return;

            this.SendOffer(acceptMessage.connectionId, configuration).Forget();
        }

        async UniTask<bool> SendOffer(string connectionId, RTCConfiguration configuration)
        {
            var pc = new RTCPeerConnection(ref configuration);
            m_mapConnectionIdAndPeer.Add(connectionId, pc);

            // create data chennel
            RTCDataChannelInit dataChannelOptions = new RTCDataChannelInit(true);
            RTCDataChannel dataChannel = pc.CreateDataChannel("dataChannel", ref dataChannelOptions);
            dataChannel.OnMessage = bytes => OnMessage(dataChannel, bytes);
            dataChannel.OnOpen = () => OnOpenChannel(connectionId, dataChannel);
            dataChannel.OnClose = () => OnCloseChannel(connectionId, dataChannel);

            pc.OnDataChannel = new DelegateOnDataChannel(channel => { OnDataChannel(pc, channel); });
            pc.SetConfiguration(ref m_conf);
            pc.OnIceCandidate = new DelegateOnIceCandidate(candidate =>
            {
                this.m_signaling.SendCandidate(connectionId, candidate);
            });

            pc.OnIceConnectionChange = new DelegateOnIceConnectionChange(state =>
            {
                if (state == RTCIceConnectionState.Disconnected)
                {
                    pc.Close();
                    m_mapConnectionIdAndPeer.Remove(connectionId);

                    this.OnDisconnected();
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

            this.m_signaling.SendOffer(connectionId, pc.LocalDescription);

            return true;
        }

        void OnOffer(ISignaling signaling, DescData e)
        {
            if (m_mapConnectionIdAndPeer.ContainsKey(e.connectionId)) return;

            RTCSessionDescription sessionDescriotion;
            sessionDescriotion.type = RTCSdpType.Offer;
            sessionDescriotion.sdp = e.sdp;

            this.SendAnswer(e.connectionId, sessionDescriotion).Forget();
        }

        async UniTask<bool> SendAnswer(string connectionId, RTCSessionDescription sessionDescriotion)
        {
            var pc = new RTCPeerConnection();
            m_mapConnectionIdAndPeer.Add(connectionId, pc);

            pc.OnDataChannel = new DelegateOnDataChannel(channel => { OnDataChannel(pc, channel); });
            pc.SetConfiguration(ref m_conf);
            pc.OnIceCandidate = new DelegateOnIceCandidate(candidate =>
            {
                this.m_signaling.SendCandidate(connectionId, candidate);
            });

            pc.OnIceConnectionChange = new DelegateOnIceConnectionChange(state =>
            {
                if (state == RTCIceConnectionState.Disconnected)
                {
                    pc.Close();
                    m_mapConnectionIdAndPeer.Remove(connectionId);
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

            this.m_signaling.SendAnswer(connectionId, desc);

            return true;
        }

        void OnAnswer(ISignaling signaling, DescData e)
        {
            Debug.Log("OnAnswer");

            RTCSessionDescription desc = new RTCSessionDescription();
            desc.type = RTCSdpType.Answer;
            desc.sdp = e.sdp;

            RTCPeerConnection pc = this.m_mapConnectionIdAndPeer[e.connectionId];
            pc.SetRemoteDescription(ref desc);
        }

        void OnIceCandidate(ISignaling signaling, CandidateData e)
        {
            if (!m_mapConnectionIdAndPeer.TryGetValue(e.connectionId, out var pc))
            {
                return;
            }

            Debug.Log("OnIceCandidate");

            RTCIceCandidate​ _candidate = default;
            _candidate.candidate = e.candidate;
            _candidate.sdpMLineIndex = e.sdpMLineIndex;
            _candidate.sdpMid = e.sdpMid;

            pc.AddIceCandidate(ref _candidate);
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
            channel.OnClose = () => OnCloseChannel(this.m_signaling.m_acceptMessage.connectionId, channel);

            this.OnConnected();
        }

        /// <summary>
        /// 自分のピアで作成したDataChannelの接続が確立されたときに呼ばれる。
        /// </summary>
        /// <param name="channel"></param>
        void OnOpenChannel(string connectionId, RTCDataChannel channel)
        {
            var pc = this.m_mapConnectionIdAndPeer[connectionId];

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
            this.OnDisconnected();
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

            Debug.Log("Send Message");
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

            Debug.Log("Send Message");

            return true;
        }

        protected virtual void OnConnected()
        {

        }

        protected virtual void OnDisconnected()
        {

        }

        protected bool IsConnected()
        {
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

        RTCConfiguration GetSelectedSdpSemantics()
        {
            RTCConfiguration config = default;
            var rtcIceServers = new List<RTCIceServer>();

            foreach (var iceServer in this.m_signaling.m_acceptMessage.iceServers)
            {
                RTCIceServer rtcIceServer = new RTCIceServer();
                rtcIceServer.urls = iceServer.urls.ToArray();
                rtcIceServer.username = iceServer.username;
                rtcIceServer.credential = iceServer.credential;
                rtcIceServer.credentialType = RTCIceCredentialType.OAuth;

                rtcIceServers.Add(rtcIceServer);
            }

            config.iceServers = rtcIceServers.ToArray();

            return config;
        }
    }
}
