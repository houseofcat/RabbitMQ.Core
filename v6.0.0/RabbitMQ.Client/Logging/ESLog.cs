namespace RabbitMQ.Client.Logging
{
    public static class ESLog
    {
        public static void Info(string message)
        {
            Logging.RabbitMqClientEventSource.Log.Info(message);
        }

        public static void Info(string message, params object[] args)
        {
            string msg = string.Format(message, args);
            Info(msg);
        }

        public static void Warn(string message)
        {
            Logging.RabbitMqClientEventSource.Log.Warn(message);
        }

        public static void Warn(string message, params object[] args)
        {
            string msg = string.Format(message, args);
            Warn(msg);
        }

        public static void Error(string message, System.Exception ex)
        {
            Logging.RabbitMqClientEventSource.Log.Error(message, ex);
        }

        public static void Error(string message, System.Exception ex, params object[] args)
        {
            string msg = string.Format(message, args);
            Error(msg, ex);
        }
    }
}
