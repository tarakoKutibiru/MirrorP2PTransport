using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Mirror.WebRTC
{
    [Serializable()]
    public class MirrorP2PMessage
    {
        public readonly Type Type;
        public readonly byte[] Payload;

        MirrorP2PMessage(Type type, byte[] payload)
        {
            this.Type = type;
            this.Payload = payload;
        }

        public static MirrorP2PMessage Create<T>(T t)
        {
            return new MirrorP2PMessage(t.GetType(), Serialize<T>(t));
        }

        public static MirrorP2PMessage Create(byte[] rawData)
        {
            using (var ms = new MemoryStream(rawData))
            {
                var formatter = new BinaryFormatter();
                return formatter.Deserialize(ms) as MirrorP2PMessage;
            }
        }

        public byte[] ToRawData()
        {
            return Serialize<MirrorP2PMessage>(this);
        }

        static byte[] Serialize<T>(T instance)
        {
            using (var ms = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(ms, instance);
                return ms.ToArray();
            }
        }

        public static object Deserialize(byte[] rawData)
        {
            using (var ms = new MemoryStream(rawData))
            {
                var formatter = new BinaryFormatter();
                return formatter.Deserialize(ms);
            }
        }
    }
}
