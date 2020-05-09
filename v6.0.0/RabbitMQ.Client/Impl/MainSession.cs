// We use spec version 0-9 for common constants such as frame types,
// error codes, and the frame end byte, since they don't vary *within
// the versions we support*. Obviously we may need to revisit this if
// that ever changes.

using RabbitMQ.Client.Framing.Impl;
using System;

namespace RabbitMQ.Client.Impl
{
    ///<summary>Small ISession implementation used only for channel 0.</summary>
    class MainSession : Session
    {
        private readonly object _closingLock = new object();

        private readonly ushort _closeClassId;
        private readonly ushort _closeMethodId;
        private readonly ushort _closeOkClassId;
        private readonly ushort _closeOkMethodId;

        private bool _closeServerInitiated;
        private bool _closing;

        public MainSession(Connection connection) : base(connection, 0)
        {
            connection.Protocol.CreateConnectionClose(0, string.Empty, out Command request, out _closeOkClassId, out _closeOkMethodId);
            _closeClassId = request.Method.ProtocolClassId;
            _closeMethodId = request.Method.ProtocolMethodId;
        }

        public Action Handler { get; set; }

        public override void HandleFrame(InboundFrame frame)
        {
            lock (_closingLock)
            {
                if (!_closing)
                {
                    base.HandleFrame(frame);
                    return;
                }
            }

            if (!_closeServerInitiated && frame.IsMethod())
            {
                MethodBase method = Connection.Protocol.DecodeMethodFrom(frame.Payload);
                if ((method.ProtocolClassId == _closeClassId)
                    && (method.ProtocolMethodId == _closeMethodId))
                {
                    base.HandleFrame(frame);
                    return;
                }

                if ((method.ProtocolClassId == _closeOkClassId)
                    && (method.ProtocolMethodId == _closeOkMethodId))
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
                if (!_closing)
                {
                    _closing = true;
                    _closeServerInitiated = closeServerInitiated;
                }
            }
        }

        public override void Transmit(Command cmd)
        {
            lock (_closingLock)
            {
                if (!_closing)
                {
                    base.Transmit(cmd);
                    return;
                }
            }

            // Allow always for sending close ok
            // Or if application initiated, allow also for sending close
            MethodBase method = cmd.Method;
            if (((method.ProtocolClassId == _closeOkClassId)
                 && (method.ProtocolMethodId == _closeOkMethodId))
                || (!_closeServerInitiated &&
                    (method.ProtocolClassId == _closeClassId) &&
                    (method.ProtocolMethodId == _closeMethodId)
                    ))
            {
                base.Transmit(cmd);
            }
        }
    }
}
