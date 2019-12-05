using System;
using System.Collections.Generic;
using System.Threading;

#if NETFX_CORE
using System.Threading.Tasks;
#endif

using RabbitMQ.Client.Exceptions;
using RabbitMQ.Client.Framing.Impl;
using RabbitMQ.Util;
using RabbitMQ.Client.Framing;

namespace RabbitMQ.Client.Impl
{
    public class SessionManager
    {
        public readonly ushort ChannelMax;
        private readonly IntAllocator Ints;
        private readonly Connection m_connection;
        private readonly IDictionary<int, ISession> m_sessionMap = new Dictionary<int, ISession>();
        private bool m_autoClose = false;

        public SessionManager(Connection connection, ushort channelMax)
        {
            m_connection = connection;
            ChannelMax = (channelMax == 0) ? ushort.MaxValue : channelMax;
            Ints = new IntAllocator(1, ChannelMax);
        }

        [Obsolete("Please explicitly close connections instead.")]
        public bool AutoClose
        {
            get { return m_autoClose; }
            set
            {
                m_autoClose = value;
                CheckAutoClose();
            }
        }

        public int Count
        {
            get
            {
                lock (m_sessionMap)
                {
                    return m_sessionMap.Count;
                }
            }
        }

        ///<summary>Called from CheckAutoClose, in a separate thread,
        ///when we decide to close the connection.</summary>
        public void AutoCloseConnection()
        {
            m_connection.Abort(Constants.ReplySuccess, "AutoClose", ShutdownInitiator.Library, Timeout.Infinite);
        }

        ///<summary>If m_autoClose and there are no active sessions
        ///remaining, Close()s the connection with reason code
        ///200.</summary>
        public void CheckAutoClose()
        {
            if (m_autoClose)
            {
                lock (m_sessionMap)
                {
                    if (m_sessionMap.Count == 0)
                    {
                        // Run this in a background thread, because
                        // usually CheckAutoClose will be called from
                        // HandleSessionShutdown above, which runs in
                        // the thread of the connection. If we were to
                        // attempt to close the connection from within
                        // the connection's thread, we would suffer a
                        // deadlock as the connection thread would be
                        // blocking waiting for its own mainloop to
                        // reply to it.
#if NETFX_CORE
                        Task.Factory.StartNew(AutoCloseConnection, TaskCreationOptions.LongRunning);
#else
                        new Thread(AutoCloseConnection).Start();
#endif
                    }
                }
            }
        }

        public ISession Create()
        {
            lock (m_sessionMap)
            {
                int channelNumber = Ints.Allocate();
                if (channelNumber == -1)
                {
                    throw new ChannelAllocationException();
                }
                return CreateInternal(channelNumber);
            }
        }

        public ISession Create(int channelNumber)
        {
            lock (m_sessionMap)
            {
                if (!Ints.Reserve(channelNumber))
                {
                    throw new ChannelAllocationException(channelNumber);
                }
                return CreateInternal(channelNumber);
            }
        }

        public ISession CreateInternal(int channelNumber)
        {
            lock (m_sessionMap)
            {
                ISession session = new Session(m_connection, channelNumber);
                session.SessionShutdown += HandleSessionShutdown;
                m_sessionMap[channelNumber] = session;
                return session;
            }
        }

        public void HandleSessionShutdown(object sender, ShutdownEventArgs reason)
        {
            lock (m_sessionMap)
            {
                var session = (ISession)sender;
                m_sessionMap.Remove(session.ChannelNumber);
                Ints.Free(session.ChannelNumber);
                CheckAutoClose();
            }
        }

        public ISession Lookup(int number)
        {
            lock (m_sessionMap)
            {
                return m_sessionMap[number];
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
            lock (m_sessionMap)
            {
                ISession previous = m_sessionMap[channelNumber];
                previous.SessionShutdown -= HandleSessionShutdown;
                m_sessionMap[channelNumber] = replacement;
                replacement.SessionShutdown += HandleSessionShutdown;
                return previous;
            }
        }
    }
}
