using Ayame.Signaling;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Unity.RenderStreaming.Signaling;
using Unity.WebRTC;
using UnityEngine;

namespace Unity.RenderStreaming
{
    using DataChannelDictionary = Dictionary<int, RTCDataChannel>;

    [Serializable]
    public class ButtonClickEvent : UnityEngine.Events.UnityEvent<int> { }

    [Serializable]
    public class ButtonClickElement
    {
        [Tooltip("Specifies the ID on the HTML")]
        public int elementId;
        public ButtonClickEvent click;
    }

    public class RenderStreaming : MonoBehaviour
    {
#pragma warning disable 0649
        [SerializeField, Tooltip("Address for signaling server")]
        private string urlSignaling = "http://localhost";

        [SerializeField, Tooltip("Ayame Signaling Key")]
        private string signalingKey = "";

        [SerializeField, Tooltip("Ayame RoomId")]
        private string roomId = "";

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

        [SerializeField, Tooltip("Enable or disable hardware encoder")]
        private bool hardwareEncoderSupport = true;

        [SerializeField, Tooltip("Array to set your own click event")]
        private ButtonClickElement[] arrayButtonClickEvent;
#pragma warning restore 0649

        private AyameSignaling m_signaling;
        private readonly Dictionary<string, RTCPeerConnection> m_mapConnectionIdAndPeer = new Dictionary<string, RTCPeerConnection>();
        private readonly Dictionary<RTCPeerConnection, DataChannelDictionary> m_mapPeerAndChannelDictionary = new Dictionary<RTCPeerConnection, DataChannelDictionary>();
        private RTCConfiguration m_conf;

        public static RenderStreaming Instance { get; private set; }

        public void Awake()
        {
            Instance = this;
            var encoderType = hardwareEncoderSupport ? EncoderType.Hardware : EncoderType.Software;
            WebRTC.WebRTC.Initialize(encoderType);
        }

        public void OnDestroy()
        {
            Instance = null;
            WebRTC.WebRTC.Dispose();
            Unity.WebRTC.Audio.Stop();
        }
        public void Start()
        {
            m_conf = default;
            m_conf.iceServers = iceServers;
            StartCoroutine(WebRTC.WebRTC.Update());
        }

        void OnEnable()
        {
            if (this.m_signaling == null)
            {
                this.m_signaling = new AyameSignaling(urlSignaling, signalingKey, roomId, interval);

                this.m_signaling.OnAccept += OnAccept;
                this.m_signaling.OnAnswer += OnAnswer;
                this.m_signaling.OnOffer += OnOffer;
                this.m_signaling.OnIceCandidate += OnIceCandidate;
            }
            this.m_signaling.Start();
        }

        void OnDisable()
        {
            if (this.m_signaling != null)
            {
                this.m_signaling.Stop();
                this.m_signaling = null;
            }
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

        void OnAccept(AyameSignaling ayameSignaling)
        {
            AcceptMessage acceptMessage = ayameSignaling.m_acceptMessage;

            bool shouldSendOffer = acceptMessage.isExistClient;

            var configuration = GetSelectedSdpSemantics();
            this.iceServers = configuration.iceServers;
            m_conf.iceServers = this.iceServers;

            // wait Offer
            if (!shouldSendOffer) return;

            // TODO: Send Offer
        }

        void OnOffer(ISignaling signaling, DescData e)
        {
            RTCSessionDescription _desc;
            _desc.type = RTCSdpType.Offer;
            _desc.sdp = e.sdp;
            var connectionId = e.connectionId;
            if (m_mapConnectionIdAndPeer.ContainsKey(connectionId))
            {
                return;
            }
            var pc = new RTCPeerConnection();
            m_mapConnectionIdAndPeer.Add(e.connectionId, pc);

            pc.OnDataChannel = new DelegateOnDataChannel(channel => { OnDataChannel(pc, channel); });
            pc.SetConfiguration(ref m_conf);
            pc.OnIceCandidate = new DelegateOnIceCandidate(candidate =>
            {
                signaling.SendCandidate(e.connectionId, candidate);
            });
            pc.OnIceConnectionChange = new DelegateOnIceConnectionChange(state =>
            {
                if (state == RTCIceConnectionState.Disconnected)
                {
                    pc.Close();
                    m_mapConnectionIdAndPeer.Remove(e.connectionId);
                }
            });
            //make video bit rate starts at 16000kbits, and 160000kbits at max.
            string pattern = @"(a=fmtp:\d+ .*level-asymmetry-allowed=.*)\r\n";
            _desc.sdp = Regex.Replace(_desc.sdp, pattern, "$1;x-google-start-bitrate=16000;x-google-max-bitrate=160000\r\n");
            pc.SetRemoteDescription(ref _desc);

            RTCAnswerOptions options = default;
            var op = pc.CreateAnswer(ref options);
            while (op.MoveNext())
            {
            }
            if (op.IsError)
            {
                Debug.LogError($"Network Error: {op.Error}");
                return;
            }

            var desc = op.Desc;
            var opLocalDesc = pc.SetLocalDescription(ref desc);
            while (opLocalDesc.MoveNext())
            {
            }
            if (opLocalDesc.IsError)
            {
                Debug.LogError($"Network Error: {opLocalDesc.Error}");
                return;
            }

            signaling.SendAnswer(connectionId, desc);
        }

        void OnAnswer(ISignaling signaling, DescData e)
        {
            // TODO: Answer sdp SetRemoteDescription 
        }

        void OnIceCandidate(ISignaling signaling, CandidateData e)
        {
            if (!m_mapConnectionIdAndPeer.TryGetValue(e.connectionId, out var pc))
            {
                return;
            }

            RTCIceCandidate​ _candidate = default;
            _candidate.candidate = e.candidate;
            _candidate.sdpMLineIndex = e.sdpMLineIndex;
            _candidate.sdpMid = e.sdpMid;

            pc.AddIceCandidate(ref _candidate);
        }

        void OnDataChannel(RTCPeerConnection pc, RTCDataChannel channel)
        {
            if (!m_mapPeerAndChannelDictionary.TryGetValue(pc, out var channels))
            {
                channels = new DataChannelDictionary();
                m_mapPeerAndChannelDictionary.Add(pc, channels);
            }
            channels.Add(channel.Id, channel);

            if (channel.Label != "data")
            {
                return;
            }

            channel.OnMessage = bytes => OnMessage(channel, bytes);
            channel.OnClose = () => OnCloseChannel(channel);
        }

        void OnCloseChannel(RTCDataChannel channel)
        {

        }

        void OnMessage(RTCDataChannel channel, byte[] bytes)
        {

        }
    }
}
