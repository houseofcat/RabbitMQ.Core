using RabbitMQ.Client.Framing.Impl;
using System;

namespace RabbitMQ.Client.Impl
{
    ///<summary>Small ISession implementation used only for channel 0.</summary>
    public class MainSession : Session
    {
        private readonly object _closingLock = new object();

        public int m_closeClassId;
        public int m_closeMethodId;
        public int m_closeOkClassId;
        public int m_closeOkMethodId;

        public bool m_closeServerInitiated;
        public bool m_closing;

        public MainSession(Connection connection) : base(connection, 0)
        {
            connection.Protocol.CreateConnectionClose(0, string.Empty, out Command request, out m_closeOkClassId, out m_closeOkMethodId);
            m_closeClassId = request.Method.ProtocolClassId;
            m_closeMethodId = request.Method.ProtocolMethodId;
        }

        public Action Handler { get; set; }

        public override void HandleFrame(InboundFrame frame)
        {
            lock (_closingLock)
            {
                if (!m_closing)
                {
                    base.HandleFrame(frame);
                    return;
                }
            }

            if (!m_closeServerInitiated && (frame.IsMethod()))
            {
                MethodBase method = Connection.Protocol.DecodeMethodFrom(frame.GetReader());
                if ((method.ProtocolClassId == m_closeClassId)
                    && (method.ProtocolMethodId == m_closeMethodId))
                {
                    base.HandleFrame(frame);
                    return;
                }

                if ((method.ProtocolClassId == m_closeOkClassId)
                    && (method.ProtocolMethodId == m_closeOkMethodId))
                {
                    // This is the reply (CloseOk) we were looking for
                    // Call any listener attached to this session
                    Handler();
                }
            }

            // Either a non-method frame, or not what we were looking
            // for. Ignore it - we're quiescing.
        }

        ///<summary> Set channel 0 as quiescing </summary>
        ///<remarks>
        /// Method should be idempotent. Cannot use base.Close
        /// method call because that would prevent us from
        /// sending/receiving Close/CloseOk commands
        ///</remarks>
        public void SetSessionClosing(bool closeServerInitiated)
        {
            lock (_closingLock)
            {
                if (!m_closing)
                {
                    m_closing = true;
                    m_closeServerInitiated = closeServerInitiated;
                }
            }
        }

        public override void Transmit(Command cmd)
        {
            lock (_closingLock)
            {
                if (!m_closing)
                {
                    base.Transmit(cmd);
                    return;
                }
            }

            // Allow always for sending close ok
            // Or if application initiated, allow also for sending close
            MethodBase method = cmd.Method;
            if (((method.ProtocolClassId == m_closeOkClassId)
                 && (method.ProtocolMethodId == m_closeOkMethodId))
                || (!m_closeServerInitiated && (
                    (method.ProtocolClassId == m_closeClassId) &&
                    (method.ProtocolMethodId == m_closeMethodId))
                    ))
            {
                base.Transmit(cmd);
            }
        }
    }
}
