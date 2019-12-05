using System.Collections.Generic;

namespace RabbitMQ.Client.Impl
{
    public abstract class StreamProperties : ContentHeaderBase, IStreamProperties
    {
        public abstract string ContentEncoding { get; set; }
        public abstract string ContentType { get; set; }
        public abstract IDictionary<string, object> Headers { get; set; }
        public abstract byte Priority { get; set; }
        public abstract AmqpTimestamp Timestamp { get; set; }

        public override object Clone()
        {
            var clone = MemberwiseClone() as StreamProperties;
            if (IsHeadersPresent())
            {
                clone.Headers = new Dictionary<string, object>();
                foreach (KeyValuePair<string, object> entry in Headers)
                {
                    clone.Headers[entry.Key] = entry.Value;
                }
            }

            return clone;
        }

        public abstract void ClearContentEncoding();
        public abstract void ClearContentType();
        public abstract void ClearHeaders();
        public abstract void ClearPriority();
        public abstract void ClearTimestamp();

        public abstract bool IsContentEncodingPresent();
        public abstract bool IsContentTypePresent();
        public abstract bool IsHeadersPresent();
        public abstract bool IsPriorityPresent();
        public abstract bool IsTimestampPresent();
    }
}
