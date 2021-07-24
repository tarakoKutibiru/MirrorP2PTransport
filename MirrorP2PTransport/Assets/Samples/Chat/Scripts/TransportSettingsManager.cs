using Mirror.WebRTC;
using UnityEngine;
using UnityEngine.UI;

namespace Mirror.Examples.Chat
{
    public class TransportSettingsManager : MonoBehaviour
    {
        [SerializeField] InputField url = default;
        [SerializeField] InputField key = default;
        [SerializeField] InputField roomId = default;

        MirrorP2PTransport transport = default;

        private void Start()
        {
            this.transport = this.GetComponent<MirrorP2PTransport>();

            this.url.text = this.transport.signalingURL;
            this.key.text = this.transport.signalingKey;
            this.roomId.text = this.transport.roomId;
        }

        public void SetURL(string v)
        {
            this.transport.signalingURL = v;
        }
        public void SetKey(string v)
        {
            this.transport.signalingKey = v;
        }
        public void SetRoomId(string v)
        {
            this.transport.roomId = v;
        }
    }
}
