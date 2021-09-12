using System;

namespace Mirror.WebRTC
{
    public interface IRequest
    {
        public enum RequestType
        {
            ConnectedConfirm,
        }

        Guid Uid { get; }
        RequestType GetRequestType();
    }

}
