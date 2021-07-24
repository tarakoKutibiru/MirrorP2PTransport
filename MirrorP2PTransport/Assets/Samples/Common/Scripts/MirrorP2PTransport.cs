using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mirror.WebRTC.Samples
{
    public class MirrorP2PTransport : Mirror.WebRTC.MirrorP2PTransport
    {
        protected override void Awake()
        {
            var settings = Resources.Load<AyameSignalingSettings>("AyameSignalingSettings");

            if (settings != default)
            {
                if (string.IsNullOrEmpty(this.signalingKey)) this.signalingKey = settings.signalingKey;
                if (string.IsNullOrEmpty(this.signalingURL)) this.signalingURL = settings.signalingUrl;
                if (string.IsNullOrEmpty(this.roomId)) this.roomId = settings.roomBaseId + "_" + SceneManager.GetActiveScene().name;
            }
            else
            {
                if (string.IsNullOrEmpty(this.signalingURL)) this.signalingURL = "wss://ayame-labo.shiguredo.jp/signaling";
            }

            base.Awake();
        }
    }
}
