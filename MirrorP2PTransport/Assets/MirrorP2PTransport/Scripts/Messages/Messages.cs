
namespace Mirror.WebRTC.TransportMessages
{
    public class Message
    {
        public string type;
    }

    public class PingMessage
    {
        public static readonly string type = "ping";
    }

    public class PongMessage
    {
        public static readonly string type = "ping";
    }
}
