using System;

namespace Mirror.WebRTC
{
    [Serializable()]
    public class ConnectServerRequest : IRequest
    {
        public Guid Uid => this.uid;
        public readonly Guid uid;

        public ConnectServerRequest()
        {
            this.uid = Guid.NewGuid();
        }
    }

    [Serializable()]
    public class ConnectServerResponse : IResponse
    {
        public Guid Uid => this.uid;
        public readonly Guid uid;
        public readonly string roomId;

        public ConnectServerResponse(ConnectServerRequest request, string roomId)
        {
            this.uid = request.Uid;
            this.roomId = roomId;
        }
    }

}
