namespace Mirror.WebRTC.Test.SendMessage
{
    [System.Serializable]
    public struct Message : NetworkMessage
    {
        public string message;
        public Message(string message)
        {
            this.message = message;
        }
    }
}
