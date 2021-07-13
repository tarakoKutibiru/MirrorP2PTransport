using System.Collections.Generic;
using Unity.WebRTC;

namespace Ayame.Signaling
{
    public class Message
    {
        public string type;
    }

    public class AcceptMessage
    {
        public string          type = "accept";
        public string          connectionId;
        public List<IceServer> iceServers;
        public bool            isExistClient;
        public bool            isExistUser;

        public RTCIceServer[] ToRTCIceServers(RTCIceServer[] iceServers)
        {
            RTCIceServer[] servers = new RTCIceServer[this.iceServers.Count + iceServers.Length];
            for (int i = 0; i < this.iceServers.Count; i++)
            {
                servers[i] = this.iceServers[i].ToRTCIceServer();
            }
            for (int i = 0; i < iceServers.Length; i++)
            {
                servers[this.iceServers.Count + 0] = iceServers[i];
            }

            return servers;
        }
    }

    [System.Serializable]
    public class IceServer
    {
        public List<string> urls;
        public string       username;
        public string       credential;

        public RTCIceServer ToRTCIceServer()
        {
            RTCIceServer rtcIceServer = new RTCIceServer();
            rtcIceServer.credential = this.credential;
            rtcIceServer.username   = this.username;
            rtcIceServer.urls       = this.urls.ToArray();

            return rtcIceServer;
        }
    }

    public class RegisterMessage
    {
        public string type = "register";
        public string roomId;
        public string clientId;
        public string signalingKey;
        public string authnMetadata;
    }

    public class AnswerMessage
    {
        public string type = "answer";
        public string sdp;
    }

    public class OfferMessage
    {
        public string type = "offer";
        public string sdp;
    }

    public class CandidateMessage
    {
        public string type = "candidate";
        public Ice    ice;
    }

    [System.Serializable]
    public class Ice
    {
        public string candidate;
        public string sdpMid;
        public int    sdpMLineIndex;
    }

    public class PongMessage
    {
        public string type = "pong";
    }
}
