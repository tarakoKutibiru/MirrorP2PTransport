using System;

namespace Mirror.WebRTC
{
    public class MirrorP2PMessage
    {
        static readonly Random rand = new Random();

        public enum Type
        {
            Ping,
            Pong,
            ConnectedConfirmRequest,
            ConnectedConfirmResponce,
            RawData,
        }

        public int Uid { get; private set; }
        public Type MessageType { get; private set; }
        public byte[] rawData { get; private set; }

        private MirrorP2PMessage(int uid, Type type, byte[] rawData = default)
        {
            this.Uid = uid;
            this.MessageType = type;
            this.rawData = rawData;
        }

        static public MirrorP2PMessage CreatePingMessage()
        {
            return MirrorP2PMessage.CreateMessage(Type.Ping);
        }

        static public MirrorP2PMessage CreatePongMessage(int uid)
        {
            return new MirrorP2PMessage(uid, Type.Pong);
        }

        static public MirrorP2PMessage CreateConnectedConfirmRequest()
        {
            return MirrorP2PMessage.CreateMessage(Type.ConnectedConfirmRequest);
        }

        static public MirrorP2PMessage CreateConnectedConfirmResponce(int uid)
        {
            return new MirrorP2PMessage(uid, Type.ConnectedConfirmResponce);
        }

        static MirrorP2PMessage CreateMessage(Type type)
        {
            return new MirrorP2PMessage(rand.Next(1, Int32.MaxValue), type);
        }

        static public MirrorP2PMessage CreateRawDataMessage(byte[] rawData)
        {
            return new MirrorP2PMessage(rand.Next(1, Int32.MaxValue), Type.RawData, rawData);
        }

        static public MirrorP2PMessage LoadMessage(byte[] payload)
        {
            int uid = BitConverter.ToInt32(payload, 4);
            Type type = (Type)BitConverter.ToInt32(payload, 0);

            if (payload.Length <= 8) return new MirrorP2PMessage(uid, type);

            byte[] rawData = new byte[payload.Length - 8];
            Array.Copy(payload, 8, rawData, 0, rawData.Length);

            return new MirrorP2PMessage(uid, type, rawData);
        }

        public byte[] ToPayload()
        {
            byte[] header = new byte[8];
            Array.Copy(BitConverter.GetBytes((int)this.MessageType), header, 4);
            Array.Copy(BitConverter.GetBytes(this.Uid), 0, header, 4, 4);

            if (rawData == default)
            {
                byte[] payload = new byte[header.Length];
                Array.Copy(header, 0, payload, 0, header.Length);
                return payload;
            }
            else
            {
                byte[] payload = new byte[header.Length + rawData.Length];
                Array.Copy(header, 0, payload, 0, header.Length);
                Array.Copy(rawData, 0, payload, header.Length, rawData.Length);
                return payload;
            }
        }
    }
}
