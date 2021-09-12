using System;

namespace Mirror.WebRTC
{
    [Serializable()]
    public class ConnectedConfirmRequest : IRequest
    {
        public Guid Uid => this.uid;
        public readonly Guid uid;
        public IRequest.RequestType GetRequestType()
        {
            return IRequest.RequestType.ConnectedConfirm;
        }

        public ConnectedConfirmRequest()
        {
            this.uid = Guid.NewGuid();
        }
    }

    [Serializable()]
    public class ConnectedConfirmResponce : IResponse
    {
        public Guid Uid => this.uid;

        public readonly Guid uid;

        public ConnectedConfirmResponce(ConnectedConfirmRequest request)
        {
            this.uid = request.Uid;
        }
    }
}
