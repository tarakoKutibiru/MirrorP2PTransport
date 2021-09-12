using System;

namespace Mirror.WebRTC
{
    [Serializable()]
    public class RawData
    {
        public readonly byte[] rawData;

        public RawData(byte[] rawData)
        {
            this.rawData = rawData;
        }
    }
}
