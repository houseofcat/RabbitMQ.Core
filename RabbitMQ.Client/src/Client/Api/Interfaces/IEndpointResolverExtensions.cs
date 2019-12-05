using System;
using System.Collections.Generic;

namespace RabbitMQ.Client
{
    public static class EndpointResolverExtensions
    {
        public static T SelectOne<T>(this IEndpointResolver resolver, Func<AmqpTcpEndpoint, T> selector)
        {
            var t = default(T);
            var exceptions = new List<Exception>();
            foreach (var ep in resolver.All())
            {
                try
                {
                    t = selector(ep);
                    if (t.Equals(default(T)) == false)
                    {
                        return t;
                    }
                }
                catch (Exception e)
                {
                    exceptions.Add(e);
                }
            }

            if (Object.Equals(t, default(T)) && exceptions.Count > 0)
            {
                throw new AggregateException(exceptions);
            }

            return t;
        }
    }
}