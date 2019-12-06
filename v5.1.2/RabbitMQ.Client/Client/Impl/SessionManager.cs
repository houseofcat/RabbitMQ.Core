using RabbitMQ.Client.Exceptions;
using RabbitMQ.Client.Framing;
using RabbitMQ.Client.Framing.Impl;
using RabbitMQ.Util;
using System.Collections.Generic;
using System.Threading;

namespace RabbitMQ.Client.Impl
{
    public class SessionManager
    {
        public readonly ushort ChannelMax;
        private readonly IntAllocator Ints;
        private readonly Connection m_connection;
        private readonly IDictionary<int, ISession> m_sessionMap = new Dictionary<int, ISession>();

        public SessionManager(Connection connection, ushort channelMax)
        {
            m_connection = connection;
            ChannelMax = (channelMax == 0) ? ushort.MaxValue : channelMax;
            Ints = new IntAllocator(1, ChannelMax);
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
