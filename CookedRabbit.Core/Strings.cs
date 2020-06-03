namespace CookedRabbit.Core
{
    public static class Strings
    {
        // AutoPublisher
        public const string InitializeError = "AutoPublisher is not initialized or is shutdown.";

        // Publisher
        public const string HeaderPrefix = "X-";

        // General
        public const string QueueChannelError = "Can't queue a letter to a closed Threading.Channel.";
        public const string RangeErrorMessage = "Value for {0} must be between {1} and {2}.";
        public const string ChannelReadErrorMessage = "Can't use reader on a closed Threading.Channel.";
        public const string NoConsumerSettingsMessage = "Consumer {0} not found in ConsumerSettings dictionary.";

        public const string ValidationMessage = "ConnectionPool is not initialized or is shutdown.";
        public const string ShutdownValidationMessage = "ConnectionPool is not initialized. Can't be Shutdown.";
        public const string GetConnectionErrorMessage = "Threading.Channel used for reading RabbitMQ connections has been closed.";

        public const string ChannelPoolNotInitializedMessage = "ChannelPool is not usable until it has been initialized.";
        public const string EncrypConfigErrorMessage = "Encryption can't be enabled without a HashKey (32-byte length).";

        // ChannelPool
        public const string ChannelPoolValidationMessage = "ChannelPool is not initialized or is shutdown.";
        public const string ChannelPoolShutdownValidationMessage = "ChannelPool is not initialized. Can't be Shutdown.";
        public const string ChannelPoolGetChannelError = "Threading.Channel used for reading RabbitMQ channels has been closed.";

        // Consumer
        public const string HeaderForObjectType = "X-CR-OBJECTTYPE";
        public const string HeaderValueForReceivedMessage = "MESSAGE";
        public const string HeaderValueForReceivedLetter = "LETTER";
        public const string HeaderValueForUnknown = "UNKNOWN";

        // Pipeline Messages
        public const string NotFinalized = "Pipeline is not ready for receiving work as it has not been finalized yet.";
        public const string AlreadyFinalized = "Pipeline is already finalized and ready for use.";
        public const string CantFinalize = "Pipeline can't finalize as no steps have been added.";
        public const string InvalidAddError = "Pipeline is already finalized and you can no longer add steps.";

        public const string ChainingImpossible = "Pipelines can't be chained together as one, or both, pipelines have been finalized.";
        public const string ChainingNotMatched = "Pipelines can't be chained together as the last step function and the first step function don't align with type input or asynchronicity.";
        public const string NothingToChain = "Pipelines can't be chained together as one, or both, pipelines have no steps.";

        public const string InvalidStepFound = "Pipeline can't chain the last step to this new step. Unexpected type found on the previous step.";
    }
}
