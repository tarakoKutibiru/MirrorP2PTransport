using UnityEngine;
using UnityEngine.UI;

namespace Mirror.WebRTC.Test.SendMessage
{
    public class SendMessageTestManager : MonoBehaviour
    {
        public InputField inputField = null;
        public MirrorP2PTransport mirrorP2PTransport = null;
        public NetworkManager networkManager = null;

        private void Start()
        {
            NetworkServer.RegisterHandler<Message>(this.OnReceiveMessage, false);
            NetworkClient.RegisterHandler<Message>(this.OnReceiveMessage, false);
        }

        public void Send()
        {
            if (this.inputField == null) return;

            Message message = new Message();
            message.message = this.inputField.text;

            if (networkManager.mode == NetworkManagerMode.Host)
            {
                NetworkServer.SendToAll<Message>(message);
            }
            else
            {
                NetworkClient.Send<Message>(message);
            }
        }

        void OnReceiveMessage(NetworkConnection conn, Message receivedMessage)
        {
            Debug.Log(JsonUtility.ToJson(receivedMessage));
        }
    }
}
