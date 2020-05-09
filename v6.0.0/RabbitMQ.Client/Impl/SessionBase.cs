using RabbitMQ.Client.Exceptions;
using RabbitMQ.Client.Framing.Impl;
using System;
using System.Collections.Generic;

namespace RabbitMQ.Client.Impl
{
    abstract class SessionBase : ISession
    {
        private readonly object _shutdownLock = new object();
        private EventHandler<ShutdownEventArgs> _sessionShutdown;

        public SessionBase(Connection connection, int channelNumber)
        {
            CloseReason = null;
            Connection = connection;
            ChannelNumber = channelNumber;
            if (channelNumber != 0)
            {
                connection.ConnectionShutdown += OnConnectionShutdown;
            }
        }

        public event EventHandler<ShutdownEventArgs> SessionShutdown
        {
            add
            {
                bool ok = false;
                if (CloseReason == null)
                {
                    lock (_shutdownLock)
                    {
                        if (CloseReason == null)
                        {
                            _sessionShutdown += value;
                            ok = true;
                        }
                    }
                }
                if (!ok)
                {
                    value(this, CloseReason);
                }
            }
            remove
            {
                lock (_shutdownLock)
                {
                    _sessionShutdown -= value;
                }
            }
        }

        public int ChannelNumber { get; private set; }
        public ShutdownEventArgs CloseReason { get; set; }
        public Action<ISession, Command> CommandReceived { get; set; }
        public Connection Connection { get; private set; }

        public bool IsOpen
        {
            get { return CloseReason == null; }
        }

        public virtual void OnCommandReceived(Command cmd)
        {
            CommandReceived?.Invoke(this, cmd);
        }

        public virtual void OnConnectionShutdown(object conn, ShutdownEventArgs reason)
        {
            Close(reason);
        }

        public virtual void OnSessionShutdown(ShutdownEventArgs reason)
        {
            Connection.ConnectionShutdown -= OnConnectionShutdown;
            EventHandler<ShutdownEventArgs> handler;
            lock (_shutdownLock)
            {
                handler = _sessionShutdown;
                _sessionShutdown = null;
            }

            handler?.Invoke(this, reason);
        }

        public override string ToString()
        {
            return GetType().Name + "#" + ChannelNumber + ":" + Connection;
        }

        public void Close(ShutdownEventArgs reason)
        {
            Close(reason, true);
        }

        public void Close(ShutdownEventArgs reason, bool notify)
        {
            if (CloseReason == null)
            {
                lock (_shutdownLock)
                {
                    if (CloseReason == null)
                    {
                        CloseReason = reason;
                    }
                }
            }
            if (notify)
            {
                OnSessionShutdown(CloseReason);
            }
        }

        public abstract void HandleFrame(InboundFrame frame);

        public void Notify()
        {
            // Ensure that we notify only when session is already closed
            // If not, throw exception, since this is a serious bug in the library
            if (CloseReason == null)
            {
                lock (_shutdownLock)
                {
                    if (CloseReason == null)
                    {
                        throw new Exception("Internal Error in Session.Close");
                    }
                }
            }
            OnSessionShutdown(CloseReason);
        }

        public virtual void Transmit(Command cmd)
        {
            if (CloseReason != null)
            {
                lock (_shutdownLock)
                {
                    if (CloseReason != null)
                    {
                        if (!Connection.Protocol.CanSendWhileClosed(cmd))
                        {
                            throw new AlreadyClosedException(CloseReason);
                        }
                    }
                }
            }
            // We used to transmit *inside* the lock to avoid interleaving
            // of frames within a channel.  But that is fixed in socket frame handler instead, so no need to lock.
            cmd.Transmit(ChannelNumber, Connection);
        }
        public virtual void Transmit(IList<Command> commands)
        {
            Connection.WriteFrameSet(Command.CalculateFrames(ChannelNumber, Connection, commands));
        }
    }
}
