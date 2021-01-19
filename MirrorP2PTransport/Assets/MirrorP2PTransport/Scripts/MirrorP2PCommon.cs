namespace Mirror.WebRTC
{
    public class Common
    {
        protected enum State
        {
            Runnning,
            Stop,
        }

        protected State state { get; set; } = State.Stop;
    }
}
