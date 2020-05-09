using RabbitMQ.Client.Framing.Impl;
using System;
using System.Collections.Generic;

namespace RabbitMQ.Client.Impl
{
    interface ISession
    {
        /// <summary>
        /// Gets the channel number.
        /// </summary>
        int ChannelNumber { get; }

        /// <summary>
        /// Gets the close reason.
        /// </summary>
        ShutdownEventArgs CloseReason { get; }

        ///<summary>
        /// Single recipient - no need for multiple handlers to be informed of arriving commands.
        ///</summary>
        Action<ISession, Command> CommandReceived { get; set; }

        /// <summary>
        /// Gets the connection.
        /// </summary>
        Connection Connection { get; }

        /// <summary>
        /// Gets a value indicating whether this session is open.
        /// </summary>
        bool IsOpen { get; }

        ///<summary>
        /// Multicast session shutdown event.
        ///</summary>
        event EventHandler<ShutdownEventArgs> SessionShutdown;

        void Close(ShutdownEventArgs reason);
        void Close(ShutdownEventArgs reason, bool notify);
        void HandleFrame(InboundFrame frame);
        void Notify();
        void Transmit(Command cmd);
        void Transmit(IList<Command> cmd);
    }
}
