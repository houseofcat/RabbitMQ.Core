namespace CookedRabbit.Core
{
    public static class Strings
    {
        public const string RangeErrorMessage = "Value for {0} must be between {1} and {2}.";
        public const string ChannelReadErrorMessage = "Can't use reader on a closed Threading.Channel.";
        public const string NoConsumerSettingsMessage = "Consumer {0} not found in ConsumerSettings dictionary.";

        public const string ValidationMessage = "ConnectionPool is not initialized or is shutdown.";
        public const string ShutdownValidationMessage = "ConnectionPool is not initialized. Can't be Shutdown.";
        public const string GetConnectionErrorMessage = "Threading.Channel used for reading RabbitMQ connections has been closed.";

        public const string ChannelPoolNotInitializedMessage = "ChannelPool is not usable until it has been initialized.";
    }
}
