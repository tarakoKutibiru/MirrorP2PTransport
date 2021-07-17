namespace Mirror.WebRTC
{
    public class Common
    {
        protected enum DataChannelLabelType
        {
            Mirror,
            TranportInternal,
        }

        protected enum State
        {
            Runnning,
            Stop,
        }

        protected enum ConnectionStatus
        {
            Connecting,
            Connected,
            Disconnecting,
            Disconnected,
        }

        protected State state { get; set; } = State.Stop;
        protected ConnectionStatus connectionStatus { get; set; } = ConnectionStatus.Disconnected;
    }
}
