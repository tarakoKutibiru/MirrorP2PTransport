using Ayame.Signaling;
using System;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using Unity.WebRTC;
using UnityEngine;
using WebSocketSharp;

namespace Mirror.WebRTC
{
    public class AyameSignaling : ISignaling
    {
        public delegate void OnAcceptHandler(AyameSignaling signaling);

        private string m_url;
        private float m_timeout;
        private bool m_running;
        private Thread m_signalingThread;
        private AutoResetEvent m_wsCloseEvent;
        private WebSocket m_webSocket;

        private string clientId;
        public string ClientId => clientId;

        public string m_signalingKey { get; private set; }
        public string m_roomId { get; private set; }
        public AcceptMessage m_acceptMessage { get; private set; } = null;

        public AyameSignaling(string url, string signalingKey, string roomId, float timeout)
        {
            this.m_url = url;
            this.m_signalingKey = signalingKey;
            this.m_roomId = roomId;
            this.m_timeout = timeout;
            this.m_wsCloseEvent = new AutoResetEvent(false);

            this.clientId = RandomString(8);

            Debug.Log($"###clientId: {this.clientId}");
        }

        string RandomString(int strLength)
        {
            string result = "";
            string charSet = "0123456789";

            System.Random rand = new System.Random();
            while (strLength != 0)
            {
                result += charSet[Mathf.FloorToInt(rand.Next(0, charSet.Length - 1))];
                strLength--;
            }
            return result;
        }

        public void Start()
        {
            if (this.m_running) return;

            Debug.Log("###Start");
            this.m_running = true;
            m_signalingThread = new Thread(WSManage);
            m_signalingThread.Start();
        }

        public void Stop()
        {
            m_running = false;
            m_signalingThread.Abort();

            m_wsCloseEvent.Close();
            m_wsCloseEvent = default;
            m_webSocket?.Close();

            m_signalingThread = default;

            Debug.Log("WSStop");
        }

        public event OnAcceptHandler OnAccept;
        public event OnOfferHandler OnOffer;
#pragma warning disable 0067
        public event OnAnswerHandler OnAnswer;
#pragma warning restore 0067
        public event OnIceCandidateHandler OnIceCandidate;


        public void SendOffer(string connectionId, RTCSessionDescription offer)
        {
            Debug.Log("Signaling: SendOffer");

            OfferMessage offerMessage = new OfferMessage();
            offerMessage.sdp = offer.sdp;

            this.WSSend(JsonUtility.ToJson(offerMessage));
        }

        public void SendAnswer(string connectionId, RTCSessionDescription answer)
        {
            Debug.Log("Signaling: SendAnswer");
            AnswerMessage answerMessage = new AnswerMessage();
            answerMessage.sdp = answer.sdp;

            this.WSSend(JsonUtility.ToJson(answerMessage));
        }

        public void SendCandidate(string connectionId, RTCIceCandidate candidate)
        {
            Debug.Log("Signaling: SendCandidate");

            CandidateMessage candidateMessage = new CandidateMessage();

            Ice ice = new Ice();
            ice.candidate = candidate.Candidate;
            ice.sdpMid = candidate.SdpMid;
            ice.sdpMLineIndex = candidate.SdpMLineIndex ?? 0;

            candidateMessage.ice = ice;
            this.WSSend(JsonUtility.ToJson(candidateMessage));
        }

        public void WSManage()
        {
            while (m_running)
            {
                WSCreate();

                m_wsCloseEvent.WaitOne();

                Thread.Sleep((int)(m_timeout * 1000));
            }

            Debug.Log("Signaling: WS managing thread ended");
        }

        private void WSCreate()
        {
            Debug.Log("###WSCreate");

            m_webSocket = new WebSocket(m_url);
            if (m_url.StartsWith("wss"))
            {
                m_webSocket.SslConfiguration.EnabledSslProtocols =
                    SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12;
            }

            m_webSocket.OnOpen += WSConnected;
            m_webSocket.OnMessage += WSProcessMessage;
            m_webSocket.OnError += WSError;
            m_webSocket.OnClose += WSClosed;

            Monitor.Enter(m_webSocket);

            Debug.Log($"Signaling: Connecting WS {m_url}");
            m_webSocket.ConnectAsync();
        }

        private void WSProcessMessage(object sender, MessageEventArgs e)
        {
            var content = Encoding.UTF8.GetString(e.RawData);
            Debug.Log($"Signaling: Receiving message: {content}");

            try
            {
                var message = JsonUtility.FromJson<Message>(content);
                string type = message.type;

                switch (type)
                {
                    case "accept":
                        {
                            AcceptMessage acceptMessage = JsonUtility.FromJson<AcceptMessage>(content);
                            this.m_acceptMessage = acceptMessage;
                            this.OnAccept?.Invoke(this);
                            break;
                        }

                    case "offer":
                        {
                            OfferMessage offerMessage = JsonUtility.FromJson<OfferMessage>(content);
                            DescData descData = new DescData();
                            descData.connectionId = this.m_acceptMessage.connectionId;
                            descData.sdp = offerMessage.sdp;
                            descData.type = offerMessage.type;

                            this.OnOffer?.Invoke(this, descData);

                            break;
                        }

                    case "answer":
                        {
                            AnswerMessage answerMessage = JsonUtility.FromJson<AnswerMessage>(content);
                            DescData descData = new DescData();
                            descData.connectionId = this.m_acceptMessage.connectionId;
                            descData.sdp = answerMessage.sdp;
                            descData.type = answerMessage.type;

                            this.OnAnswer?.Invoke(this, descData);

                            break;
                        }

                    case "candidate":
                        {
                            CandidateMessage candidateMessage = JsonUtility.FromJson<CandidateMessage>(content);

                            CandidateData candidateData = new CandidateData();
                            candidateData.connectionId = this.m_acceptMessage.connectionId;
                            candidateData.candidate = candidateMessage.ice.candidate;
                            candidateData.sdpMLineIndex = candidateMessage.ice.sdpMLineIndex;
                            candidateData.sdpMid = candidateMessage.ice.sdpMid;

                            this.OnIceCandidate?.Invoke(this, candidateData);

                            break;
                        }

                    case "ping":
                        {
                            if (this.m_running == false) return;
                            PongMessage pongMessage = new PongMessage();
                            this.WSSend(JsonUtility.ToJson(pongMessage));

                            break;
                        }

                    case "bye":
                        {
                            // TODO:
                            break;
                        }

                    default:
                        {
                            Debug.LogError("Signaling: Received message from unknown peer");
                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("Signaling: Failed to parse message: " + ex);

                throw ex;
            }
        }

        private void WSConnected(object sender, EventArgs e)
        {
            RegisterMessage registerMessage = new RegisterMessage();
            registerMessage.roomId = this.m_roomId;
            registerMessage.signalingKey = this.m_signalingKey;
            registerMessage.clientId = this.clientId;

            Debug.Log("Signaling: WS connected.");
            this.WSSend(JsonUtility.ToJson(registerMessage));
        }

        private void WSError(object sender, ErrorEventArgs e)
        {
            Debug.LogError($"Signaling: WS connection error: {e.Message}");
        }

        private void WSClosed(object sender, CloseEventArgs e)
        {
            Debug.LogError($"Signaling: WS connection closed, code: {e.Code}");

            m_wsCloseEvent?.Set();
            m_webSocket = null;
        }

        private void WSSend(object data)
        {
            if (m_webSocket == null || m_webSocket.ReadyState != WebSocketState.Open)
            {
                Debug.LogError("Signaling: WS is not connected. Unable to send message");
                return;
            }

            if (data is string s)
            {
                Debug.Log("Signaling: Sending WS data: " + s);
                m_webSocket.Send(s);
            }
            else
            {
                string str = JsonUtility.ToJson(data);
                Debug.Log("Signaling: Sending WS data: " + str);
                m_webSocket.Send(str);
            }
        }

    }
}
