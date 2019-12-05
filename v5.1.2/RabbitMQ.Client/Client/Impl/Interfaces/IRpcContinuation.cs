namespace RabbitMQ.Client.Impl
{
    public interface IRpcContinuation
    {
        void HandleCommand(Command cmd);
        void HandleModelShutdown(ShutdownEventArgs reason);
    }
}
