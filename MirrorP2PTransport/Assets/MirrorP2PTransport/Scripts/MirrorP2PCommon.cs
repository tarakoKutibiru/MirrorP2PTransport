using Ayame.Signaling;
using System.Collections.Generic;
using Unity.RenderStreaming;
using Unity.RenderStreaming.Signaling;
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

        public void Stop()
        {
            if (this.m_signaling != null)
            {
                this.m_signaling.Stop();
                this.m_signaling = null;
            }
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

            // send Offer
            var pc = new RTCPeerConnection(ref configuration);
            m_mapConnectionIdAndPeer.Add(acceptMessage.connectionId, pc);

            // create data chennel
            RTCDataChannelInit dataChannelOptions = new RTCDataChannelInit(true);
            RTCDataChannel dataChannel = pc.CreateDataChannel("dataChannel", ref dataChannelOptions);
            dataChannel.OnMessage = bytes => OnMessage(dataChannel, bytes);
            dataChannel.OnOpen = () => OnOpenChannel(dataChannel);
            dataChannel.OnClose = () => OnCloseChannel(dataChannel);
            var channels = new DataChannelDictionary();
            channels.Add(dataChannel.Id, dataChannel);
            this.m_mapPeerAndChannelDictionary.Add(pc, channels);

            pc.OnDataChannel = new DelegateOnDataChannel(channel => { OnDataChannel(pc, channel); });
            pc.SetConfiguration(ref m_conf);
            pc.OnIceCandidate = new DelegateOnIceCandidate(candidate =>
            {
                Debug.Log("PC OnIceCandidate");

                ayameSignaling.SendCandidate(acceptMessage.connectionId, candidate);
            });

            pc.OnIceConnectionChange = new DelegateOnIceConnectionChange(state =>
            {
                Debug.LogFormat("OnIceConnectionChange {0}", state);

                if (state == RTCIceConnectionState.Disconnected)
                {
                    pc.Close();
                    m_mapConnectionIdAndPeer.Remove(acceptMessage.connectionId);
                }
            });

            RTCOfferOptions options = new RTCOfferOptions();
            options.iceRestart = false;
            options.offerToReceiveAudio = false;
            options.offerToReceiveVideo = false;

            var opLocalDesc = pc.CreateOffer(ref options);
            while (opLocalDesc.MoveNext())
            {
            }
            if (opLocalDesc.IsError)
            {
                Debug.LogError($"Network Error: {opLocalDesc.Error}");
                return;
            }

            var desc = opLocalDesc.Desc;
            pc.SetLocalDescription(ref desc);

            ayameSignaling.SendOffer(acceptMessage.connectionId, pc.LocalDescription);
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

            // create data chennel
            /*            RTCDataChannelInit dataChannelOptions = new RTCDataChannelInit(true);
                        RTCDataChannel dataChannel = pc.CreateDataChannel("dataChannel", ref dataChannelOptions);
                        dataChannel.OnMessage = bytes => OnMessage(dataChannel, bytes);
                        dataChannel.OnOpen = () => OnOpenChannel(dataChannel);
                        dataChannel.OnClose = () => OnCloseChannel(dataChannel);
                        var channels = new DataChannelDictionary();
                        channels.Add(dataChannel.Id, dataChannel);
                        this.m_mapPeerAndChannelDictionary.Add(pc, channels);*/

            pc.OnDataChannel = new DelegateOnDataChannel(channel => { OnDataChannel(pc, channel); });
            pc.SetConfiguration(ref m_conf);
            pc.OnIceCandidate = new DelegateOnIceCandidate(candidate =>
            {
                Debug.Log("PC OnIceCandidate");
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

            //  this.m_signaling.SendCandidate(e.connectionId, _candidate);
        }

        void OnDataChannel(RTCPeerConnection pc, RTCDataChannel channel)
        {
            if (!m_mapPeerAndChannelDictionary.TryGetValue(pc, out var channels))
            {
                channels = new DataChannelDictionary();
                m_mapPeerAndChannelDictionary.Add(pc, channels);
            }
            channels.Add(channel.Id, channel);

            /*            if (channel.Label != "dataChannel")
                        {
                            return;
                        }*/

            Debug.Log("OnDataChannel");

            channel.OnMessage = bytes => OnMessage(channel, bytes);
            channel.OnClose = () => OnCloseChannel(channel);
        }

        void OnOpenChannel(RTCDataChannel channel)
        {
            Debug.Log("OnOpenChannel");
        }

        void OnCloseChannel(RTCDataChannel channel)
        {
            Debug.Log("OnCloneChannel");
        }

        protected void OnMessage(RTCDataChannel channel, byte[] bytes)
        {
            string text = System.Text.Encoding.UTF8.GetString(bytes);
            this.OnMessage(text);

            /*            //ASCII エンコード
                        text = System.Text.Encoding.ASCII.GetString(bytes);
                        Debug.LogFormat("OnMessage {0}", text);

                        //データがShift-JISの場合
                        text = System.Text.Encoding.GetEncoding("shift_jis").GetString(bytes);
                        Debug.LogFormat("OnMessage {0}", text);

                        //データがEUCの場合
                        text = System.Text.Encoding.GetEncoding("euc-jp").GetString(bytes);
                        Debug.LogFormat("OnMessage {0}", text);

                        //データがunicodeの場合
                        text = System.Text.Encoding.Unicode.GetString(bytes);
                        Debug.LogFormat("OnMessage {0}", text);*/
        }

        protected void OnMessage(string message)
        {
            Debug.LogFormat("OnMessage {0}", message);
        }

        public void SendMessage(string message)
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(message);
            this.SendMessage(bytes);
        }

        protected bool SendMessage(byte[] bytes)
        {
            RTCDataChannel dataChannel = this.GetDataChannel("dataChannel");
            if (dataChannel == null) return false;

            dataChannel.Send(bytes);

            Debug.Log("Send Message");

            return true;
        }

        protected bool IsConnected()
        {
            RTCDataChannel dataChannel = this.GetDataChannel("dataChannel");
            if (dataChannel == null) return false;

            return true;
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
