namespace Mirror.WebRTC
{
    public class MirrorP2PServer : Common
    {
        public void Start(string signalingURL, string signalingKey, string roomId)
        {
            this.signalingURL = signalingURL;
            this.signalingKey = signalingKey;
            this.roomId = roomId;

            base.Start();
        }

        public void Stop()
        {
            base.Stop();
        }

        public bool Send(int connectionId, byte[] data)
        {
            return base.SendMessage(data);
        }

        public bool Disconnect(int connectionId)
        {
            return false;
        }

        public bool IsAlive()
        {
            return base.IsConnected();
        }
    }
}
