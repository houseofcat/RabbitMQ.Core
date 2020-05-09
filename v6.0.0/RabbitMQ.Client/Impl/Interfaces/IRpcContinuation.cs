namespace RabbitMQ.Client.Impl
{
    internal interface IRpcContinuation
    {
        void HandleCommand(Command cmd);
        void HandleModelShutdown(ShutdownEventArgs reason);
    }
}
