using UnityEngine;

namespace Mirror.WebRTC.Samples
{
    /// <summary>
    /// Signaling Server "Ayame" settings
    /// </summary>
    [CreateAssetMenu(fileName = "AyameSignalingSettings", menuName = "MirrorP2PTransport/AyameSignalingSettings")]
    public class AyameSignalingSettings : ScriptableObject
    {
        public string signalingUrl;
        public string signalingKey;
        public string roomBaseId;
        public string roomId;
    }
}
