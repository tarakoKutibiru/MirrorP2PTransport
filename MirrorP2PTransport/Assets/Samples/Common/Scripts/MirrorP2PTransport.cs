using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mirror.WebRTC.Samples
{
    public class MirrorP2PTransport : Mirror.WebRTC.MirrorP2PTransport
    {
        static AyameSignalingSettings settings = default;
        public static AyameSignalingSettings GetSettings()
        {
            if (MirrorP2PTransport.settings == default)
            {
                MirrorP2PTransport.settings = Resources.Load<AyameSignalingSettings>("AyameSignalingSettings");
                if (MirrorP2PTransport.settings == default)
                {
                    MirrorP2PTransport.settings = ScriptableObject.CreateInstance("AyameSignalingSettings") as AyameSignalingSettings;
                    MirrorP2PTransport.settings.signalingUrl = "wss://ayame-labo.shiguredo.jp/signaling";
                }
            }

            return MirrorP2PTransport.settings;
        }

        protected override void Awake()
        {
            var settings = MirrorP2PTransport.GetSettings();

            if (settings != default)
            {
                if (string.IsNullOrEmpty(this.signalingKey)) this.signalingKey = settings.signalingKey;
                if (string.IsNullOrEmpty(this.signalingURL)) this.signalingURL = settings.signalingUrl;

                if (!string.IsNullOrEmpty(settings.roomId))
                {
                    if (string.IsNullOrEmpty(this.roomId)) this.roomId = settings.roomId;
                }
                else
                {
                    if (string.IsNullOrEmpty(this.roomId)) this.roomId = settings.roomBaseId + "_" + SceneManager.GetActiveScene().name;
                }

            }
            else
            {
                if (string.IsNullOrEmpty(this.signalingURL)) this.signalingURL = "wss://ayame-labo.shiguredo.jp/signaling";
            }

            base.Awake();
        }
    }
}
