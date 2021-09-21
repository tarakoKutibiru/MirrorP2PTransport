#if !UNITY_WEBGL
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.WebRTC;
using Ayame.Signaling;
using Cysharp.Threading.Tasks;

namespace Mirror.WebRTC
{
    public class AyameConnectionImpl : IAyameConnectionImpl<AyameConnectionImpl>, System.IDisposable
    {
        public AyameConnectionImplConstants.OnMessageDelegate OnMessageHandler { get; set; }
        public AyameConnectionImplConstants.OnConnectedDelegate OnConnectedHandler { get; set; }
        public AyameConnectionImplConstants.OnDisconnectedDelegate OnDisconnectedHandler { get; set; }

        AyameConnectionImplConstants.DataChannelSetting[] dataChannelSettings = default;

        AyameSignaling signaling = default;
        RTCConfiguration rtcConfiguration = default;
        RTCPeerConnection peerConnection = default;
        List<RTCDataChannel> dataChannels = default;

        public void Dispose()
        {
            this.OnMessageHandler = default;
            this.OnConnectedHandler = default;
            this.OnDisconnectedHandler = default;

            this.signaling?.Dispose();
        }

        public void Connect(AyameConnectionImplConstants.ConnectSetting setting)
        {
            this.dataChannelSettings = setting.DataChannelSettings;
            this.dataChannels = new List<RTCDataChannel>();

            this.signaling = new AyameSignaling(setting.SignalingURL, setting.SignalingKey, setting.RoomId, setting.TimeOut);
            this.signaling.OnAccept += OnAccept;
            this.signaling.OnAnswer += OnAnswer;
            this.signaling.OnOffer += OnOffer;
            this.signaling.OnIceCandidate += OnIceCandidate;

            this.rtcConfiguration = new RTCConfiguration();
            var urls = new string[]
            {
                "stun:stun.l.google.com:19302",
                "stun:stun1.l.google.com:19302",
                "stun:stun2.l.google.com:19302",
                "stun:stun3.l.google.com:19302"
            };
            this.rtcConfiguration.iceServers = new[] { new RTCIceServer { urls = urls } };

            this.signaling.Start();
        }

        public void Disconnect()
        {
            // Close DataChannel
            if (this.dataChannels != default)
            {
                foreach (var dataChannel in this.dataChannels)
                {
                    if (dataChannel.ReadyState == RTCDataChannelState.Closed) continue;
                    dataChannel.Close();
                }
            }

            // Close PeerConnection
            if (this.peerConnection != default)
            {
                this.peerConnection.Close();
            }

            if (this.signaling != default)
            {
                this.signaling.Stop();
            }

            Debug.Log("Disconnect");

            this.signaling = default;
            this.dataChannels = default;
            this.rtcConfiguration = default;
            this.peerConnection = default;
        }

        void OnDisconnected()
        {
            Debug.Log("OnDisconnected");

            this.OnDisconnectedHandler?.Invoke();
        }

        public void SendMessage(string dataChannelLabel, byte[] data)
        {
            foreach (var dataChannel in this.dataChannels)
            {
                if (dataChannel.Label != dataChannelLabel) continue;
                dataChannel.Send(data);
            }
        }

        public bool IsConnected(string dataChannelLabel)
        {
            foreach (var dataChannel in this.dataChannels)
            {
                if (dataChannel.Label != dataChannelLabel) continue;
                if (dataChannel.ReadyState == RTCDataChannelState.Open) return true;
            }

            return false;
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

            this.peerConnection?.AddIceCandidate(iceCandidate);
        }

        RTCPeerConnection CreatePeerConnection(string connectionId, RTCConfiguration rtcConfiguration)
        {
            var pc = new RTCPeerConnection(ref rtcConfiguration);

            foreach (var dataChannelSetting in this.dataChannelSettings)
            {
                RTCDataChannelInit dataChannelInit = new RTCDataChannelInit();
                dataChannelInit.ordered = true;
                dataChannelInit.negotiated = true;
                dataChannelInit.id = dataChannelSetting.Id;

                RTCDataChannel dataChannel = pc.CreateDataChannel(dataChannelSetting.Label, dataChannelInit);
                dataChannel.OnOpen += () => this.OnConnectedDataChannel(dataChannel);
            }

            pc.OnDataChannel = channel => this.OnConnectedDataChannel(channel);
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

        void OnConnectedDataChannel(RTCDataChannel dataChannel)
        {
            dataChannel.OnMessage += bytes => this.OnMessageHandler?.Invoke(dataChannel.Label, bytes);
            dataChannel.OnClose += () =>
            {
                Debug.Log("DataChannelClose");
                this.dataChannels?.Remove(dataChannel);
            };
            this.dataChannels.Add(dataChannel);

            Debug.Log($"label: {dataChannel.Label},id: {dataChannel.Id}");

            this.OnConnectedHandler?.Invoke(new AyameConnectionImplConstants.DataChannelSetting(dataChannel.Id, dataChannel.Label));
        }
        #endregion
    }
}
#endif
