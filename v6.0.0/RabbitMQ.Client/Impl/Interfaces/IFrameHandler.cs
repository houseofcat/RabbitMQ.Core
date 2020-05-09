using System;
using System.Collections.Generic;
using System.Net;

namespace RabbitMQ.Client.Impl
{
    interface IFrameHandler
    {
        AmqpTcpEndpoint Endpoint { get; }

        EndPoint LocalEndPoint { get; }

        int LocalPort { get; }

        EndPoint RemoteEndPoint { get; }

        int RemotePort { get; }

        ///<summary>Socket read timeout. System.Threading.Timeout.InfiniteTimeSpan signals "infinity".</summary>
        TimeSpan ReadTimeout { set; }

        ///<summary>Socket write timeout. System.Threading.Timeout.InfiniteTimeSpan signals "infinity".</summary>
        TimeSpan WriteTimeout { set; }

        void Close();

        ///<summary>Read a frame from the underlying
        ///transport. Returns null if the read operation timed out
        ///(see Timeout property).</summary>
        InboundFrame ReadFrame();

        void SendHeader();

        void WriteFrame(OutboundFrame frame, bool flush = true);

        void WriteFrameSet(IList<OutboundFrame> frames);
    }
}
