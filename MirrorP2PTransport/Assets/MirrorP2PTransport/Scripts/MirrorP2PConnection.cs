using Unity.WebRTC;

namespace Mirror.WebRTC
{
    public class MirrorP2PConnection
    {
        string signalingURL;
        string signalingKey;
        string roomId;

        public enum State
        {
            Running,
            Stop,
        }

        State state = State.Stop;

        public MirrorP2PConnection(string signalingURL, string signalingKey, string roomId)
        {
            this.signalingKey = signalingKey;
            this.signalingURL = signalingURL;
            this.roomId = roomId;
        }

        public void Connect()
        {
            this.state = State.Running;

            // TODO: Connect to signaling server
        }

        public void Disconnect()
        {
            // TODO: Disconnect to signaling server
        }

        public bool IsConnected()
        {
            // TODO:
            return false;
        }

        public bool SendMessage(byte[] bytes)
        {
            // TODO: Send Data by DataChannel

            return true;
        }

        void OnMessage(RTCDataChannel dataChannel, byte[] bytes)
        {
            // TODO Invoke Event
        }

        void OnConnected()
        {
            // TODO: Invoke OnConnected Event
        }

        void OnDisconnected()
        {
            // TODO: Invoke OnDisconnected Event
        }

        #region signaling
        void OnAccept()
        {

        }

        void SendOffer()
        {

        }

        void OnOffer()
        {

        }

        void SendAnswer()
        {

        }

        void OnAnswer()
        {

        }

        void OnIceCandidate()
        {

        }

        #region DataChannel
        void OnDataChannel()
        {

        }

        void OnOpenChannel()
        {

        }

        void OnCloseChannel()
        {

        }
        #endregion

        #endregion
    }
}
