using RabbitMQ.Client.Exceptions;
using RabbitMQ.Client.Framing;
using RabbitMQ.Client.Framing.Impl;
using RabbitMQ.Util;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RabbitMQ.Client.Impl
{
    class SessionManager
    {
        public readonly ushort ChannelMax;
        private readonly IntAllocator _ints;
        private readonly Connection _connection;
        private readonly IDictionary<int, ISession> _sessionMap = new Dictionary<int, ISession>();
        private bool _autoClose = false;

        public SessionManager(Connection connection, ushort channelMax)
        {
            _connection = connection;
            ChannelMax = (channelMax == 0) ? ushort.MaxValue : channelMax;
            _ints = new IntAllocator(1, ChannelMax);
        }

        [Obsolete("Please explicitly close connections instead.")]
        public bool AutoClose
        {
            get { return _autoClose; }
            set
            {
                _autoClose = value;
                CheckAutoClose();
            }
        }

        public int Count
        {
            get
            {
                lock (_sessionMap)
                {
                    return _sessionMap.Count;
                }
            }
        }

        ///<summary>Called from CheckAutoClose, in a separate thread,
        ///when we decide to close the connection.</summary>
        public void AutoCloseConnection()
        {
            _connection.Abort(Constants.ReplySuccess, "AutoClose", ShutdownInitiator.Library, Timeout.InfiniteTimeSpan);
        }

        ///<summary>If m_autoClose and there are no active sessions
        ///remaining, Close()s the connection with reason code
        ///200.</summary>
        public void CheckAutoClose()
        {
            if (_autoClose)
            {
                lock (_sessionMap)
                {
                    if (_sessionMap.Count == 0)
                    {
                        // Run this in a separate task, because
                        // usually CheckAutoClose will be called from
                        // HandleSessionShutdown above, which runs in
                        // the thread of the connection. If we were to
                        // attempt to close the connection synchronously,
                        // we would suffer a deadlock as the connection thread
                        // would be blocking waiting for its own mainloop
                        // to reply to it.
                        Task.Run((Action)AutoCloseConnection).Wait();
                    }
                }
            }
        }

        public ISession Create()
        {
            lock (_sessionMap)
            {
                int channelNumber = _ints.Allocate();
                if (channelNumber == -1)
                {
                    throw new ChannelAllocationException();
                }
                return CreateInternal(channelNumber);
            }
        }

        public ISession Create(int channelNumber)
        {
            lock (_sessionMap)
            {
                if (!_ints.Reserve(channelNumber))
                {
                    throw new ChannelAllocationException(channelNumber);
                }
                return CreateInternal(channelNumber);
            }
        }

        public ISession CreateInternal(int channelNumber)
        {
            lock (_sessionMap)
            {
                ISession session = new Session(_connection, channelNumber);
                session.SessionShutdown += HandleSessionShutdown;
                _sessionMap[channelNumber] = session;
                return session;
            }
        }

        public void HandleSessionShutdown(object sender, ShutdownEventArgs reason)
        {
            lock (_sessionMap)
            {
                var session = (ISession)sender;
                _sessionMap.Remove(session.ChannelNumber);
                _ints.Free(session.ChannelNumber);
                CheckAutoClose();
            }
        }

        public ISession Lookup(int number)
        {
            lock (_sessionMap)
            {
                return _sessionMap[number];
            }
        }

        ///<summary>Replace an active session slot with a new ISession
        ///implementation. Used during channel quiescing.</summary>
        ///<remarks>
        /// Make sure you pass in a channelNumber that's currently in
        /// use, as if the slot is unused, you'll get a null pointer
        /// exception.
        ///</remarks>
        public ISession Swap(int channelNumber, ISession replacement)
        {
            lock (_sessionMap)
            {
                ISession previous = _sessionMap[channelNumber];
                previous.SessionShutdown -= HandleSessionShutdown;
                _sessionMap[channelNumber] = replacement;
                replacement.SessionShutdown += HandleSessionShutdown;
                return previous;
            }
        }
    }
}
