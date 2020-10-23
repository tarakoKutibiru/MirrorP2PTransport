using UnityEngine;
using UnityEngine.UI;

namespace Mirror.WebRTC.Test.SendMessage
{
    public class SendMessageTestManager : MonoBehaviour
    {
        public InputField inputField = null;

        public void Send()
        {
            if (this.inputField == null) return;

            Message message = new Message();
            message.message = this.inputField.text;

            NetworkClient.Send(message);
        }
    }
}
