using System;

namespace RabbitMQ.Client
{
    /// <summary>
    /// Single entry object in the shutdown report that encapsulates description
    /// of the error which occured during shutdown.
    /// </summary>
    public class ShutdownReportEntry
    {
        public ShutdownReportEntry(string description, Exception exception)
        {
            Description = description;
            Exception = exception;
        }

        /// <summary>
        /// Description provided in the error.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// <see cref="Exception"/> object that occured during shutdown, or null if unspecified.
        /// </summary>
        public Exception Exception { get; set; }

        public override string ToString()
        {
            string output = "Message: " + Description;
            return (Exception != null) ? output + " Exception: " + Exception : output;
        }
    }
}
