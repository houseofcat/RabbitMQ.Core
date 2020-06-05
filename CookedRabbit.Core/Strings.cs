namespace CookedRabbit.Core
{
    public static class Constants
    {
        // Publisher
        public const string HeaderPrefix = "X-";

        // Consumer
        public const string HeaderForObjectType = "X-CR-OBJECTTYPE";
        public const string HeaderValueForMessage = "MESSAGE";
        public const string HeaderValueForLetter = "LETTER";
        public const string HeaderValueForUnknown = "UNKNOWN";

        public const string RangeErrorMessage = "Value for {0} must be between {1} and {2}.";
    }

    public static class ExceptionMessages
    {
        // AutoPublisher
        public readonly static string InitializeError = "AutoPublisher is not initialized or is shutdown.";

        // General
        public readonly static string QueueChannelError = "Can't queue a letter to a closed Threading.Channel.";

        public readonly static string ChannelReadErrorMessage = "Can't use reader on a closed Threading.Channel.";
        public readonly static string NoConsumerSettingsMessage = "Consumer {0} not found in ConsumerSettings dictionary.";

        public readonly static string ValidationMessage = "ConnectionPool is not initialized or is shutdown.";
        public readonly static string ShutdownValidationMessage = "ConnectionPool is not initialized. Can't be Shutdown.";
        public readonly static string GetConnectionErrorMessage = "Threading.Channel used for reading RabbitMQ connections has been closed.";

        public readonly static string ChannelPoolNotInitializedMessage = "ChannelPool is not usable until it has been initialized.";
        public readonly static string EncrypConfigErrorMessage = "Encryption can't be enabled without a HashKey (32-byte length).";

        // ChannelPool
        public readonly static string ChannelPoolValidationMessage = "ChannelPool is not initialized or is shutdown.";
        public readonly static string ChannelPoolShutdownValidationMessage = "ChannelPool is not initialized. Can't be Shutdown.";
        public readonly static string ChannelPoolGetChannelError = "Threading.Channel used for reading RabbitMQ channels has been closed.";

        // Pipeline Messages
        public readonly static string NotFinalized = "Pipeline is not ready for receiving work as it has not been finalized yet.";
        public readonly static string AlreadyFinalized = "Pipeline is already finalized and ready for use.";
        public readonly static string CantFinalize = "Pipeline can't finalize as no steps have been added.";
        public readonly static string InvalidAddError = "Pipeline is already finalized and you can no longer add steps.";

        public readonly static string ChainingImpossible = "Pipelines can't be chained together as one, or both, pipelines have been finalized.";
        public readonly static string ChainingNotMatched = "Pipelines can't be chained together as the last step function and the first step function don't align with type input or asynchronicity.";
        public readonly static string NothingToChain = "Pipelines can't be chained together as one, or both, pipelines have no steps.";

        public readonly static string InvalidStepFound = "Pipeline can't chain the last step to this new step. Unexpected type found on the previous step.";
    }

    public static class LogMessages
    {
        public static class ConnectionPool
        {
            public readonly static string Initialization = "ConnectionPool initialize call was made.";
            public readonly static string InitializationComplete = "ConnectionPool initialized.";
            public readonly static string CreateConnectionException = "Connection () failed to be created.";
            public readonly static string Shutdown = "ConnectionPool shutdown was called.";
            public readonly static string ShutdownComplete = "ConnectionPool shutdown complete.";
        }

        public static class ChannelPool
        {
            public readonly static string Initialization = "ChannelPool initialize call was made.";
            public readonly static string InitializationComplete = "ChannelPool initialized.";
            public readonly static string DeadChannel = "A dead channel ({0}) was detected... attempting to repair indefinitely.";
            public readonly static string DeadChannelRepair = "The channel host ({0}) repair loop is executing an iteration...";
            public readonly static string DeadChannelRepairConnection = "The channel host ({0}) failed because Connection unhealthy.";
            public readonly static string DeadChannelRepairConstruction = "The channel host ({0}) failed because ChannelHost construction threw exception.";
            public readonly static string DeadChannelRepairSleep = "The channel host ({0}) repair loop iteration failed. Sleeping...";
            public readonly static string DeadChannelRepairSuccess = "The channel host ({0}) repair loop finished. Channel restored and flags removed.";
            public readonly static string ReturningChannel = "The channel host ({0}) was returned to the pool. Flagged? {1}";
            public readonly static string Shutdown = "ChannelPool shutdown was called.";
            public readonly static string ShutdownComplete = "ChannelPool shutdown complete.";
        }

        public static class Publisher
        {
            public readonly static string PublishFailed = "Publish to route ({0}) failed, flagging channel host. Error: {1}";
            public readonly static string PublishLetterFailed = "Publish to route ({0}) failed [LetterId: {1}] flagging channel host. Error: {2}";
            public readonly static string PublishBatchFailed = "Batch publish failed, flagging channel host. Error: {0}";
        }

        public static class AutoPublisher
        {
            public readonly static string LetterQueued = "AutoPublisher queued letter [LetterId:{0} InternalId:{1}].";
        }
    }
}
