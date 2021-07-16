#if !UNITY_WEBGL
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.WebRTC;
using Ayame.Signaling;
using Cysharp.Threading.Tasks;

namespace Mirror.WebRTC
{
    public class AyameConnectionImpl : IAyameConnectionImpl
    {
        public OnMessageDelegate OnMessageHandler { get; set; }
        public OnConnectedDelegate OnConnectedHandler { get; set; }
        public OnDisconnectedDelegate OnDisconnectedHandler { get; set; }

        string[] dataChannelLabels;

        AyameSignaling signaling = default;
        RTCConfiguration rtcConfiguration = default;
        RTCPeerConnection peerConnection = default;
        List<RTCDataChannel> dataChannels = default;
        List<int> dataChannelIds = default;

        UniTaskCompletionSource<bool> utcs = default;

        public async UniTask<List<int>> Connect(string signalingURL, string signalingKey, string roomId, string[] dataChannelLabels, float timeOut)
        {
            this.dataChannelLabels = dataChannelLabels;
            this.dataChannels = new List<RTCDataChannel>();
            this.dataChannelIds = new List<int>();

            this.signaling = new AyameSignaling(signalingURL, signalingKey, roomId, timeOut);
            this.signaling.OnAccept += OnAccept;
            this.signaling.OnAnswer += OnAnswer;
            this.signaling.OnOffer += OnOffer;
            this.signaling.OnIceCandidate += OnIceCandidate;

            this.rtcConfiguration = new RTCConfiguration();
            this.rtcConfiguration.iceServers = new[] { new RTCIceServer { urls = new[] { "stun:stun.l.google.com:19302" } } };

            this.utcs = new UniTaskCompletionSource<bool>();
            this.signaling.Start();

            var result = await this.utcs.Task;
            if (result)
            {
                return this.dataChannelIds;
            }
            else
            {
                return default;
            }
        }

        public void Disconnect()
        {
            if (this.peerConnection == default) return;
            this.peerConnection.Close();
            Debug.Log("Disconnect");
        }

        void OnDisconnected()
        {
            this.signaling.Stop();
            this.signaling = default;
            this.dataChannels = default;
            this.rtcConfiguration = default;
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

            RTCDataChannelInit dataChannelInit = new RTCDataChannelInit();
            dataChannelInit.ordered = true;

            foreach (var label in this.dataChannelLabels)
            {
                RTCDataChannel dataChannel = pc.CreateDataChannel(label, dataChannelInit);
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
                this.dataChannels.Remove(dataChannel);
            };
            this.dataChannels.Add(dataChannel);

            Debug.Log($"label: {dataChannel.Label},id: {dataChannel.Id}");
        }
        #endregion
    }
}
#endif
