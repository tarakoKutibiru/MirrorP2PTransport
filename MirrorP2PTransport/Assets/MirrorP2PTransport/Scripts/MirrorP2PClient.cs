namespace Mirror.WebRTC
{
    public class MirrorP2PClient : Common
    {
        public void Connect(string signalingURL, string signalingKey, string roomId)
        {
            this.signalingURL = signalingURL;
            this.signalingKey = signalingKey;
            this.roomId = roomId;

            base.Start();
        }

        public void Disconnect()
        {
            base.Stop();
        }

        public bool Send(byte[] data)
        {
            return base.SendMessage(data);
        }

        public bool Connected()
        {
            return false;
        }
    }
}
