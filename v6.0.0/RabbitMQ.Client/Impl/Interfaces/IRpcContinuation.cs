namespace RabbitMQ.Client.Impl
{
    interface IRpcContinuation
    {
        void HandleCommand(Command cmd);
        void HandleModelShutdown(ShutdownEventArgs reason);
    }
}
