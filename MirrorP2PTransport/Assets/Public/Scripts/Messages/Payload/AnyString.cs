using System;

namespace Mirror.WebRTC
{
    [Serializable()]
    public class AnyString
    {
        public readonly string message;

        public AnyString(string message)
        {
            this.message = message;
        }
    }
}
