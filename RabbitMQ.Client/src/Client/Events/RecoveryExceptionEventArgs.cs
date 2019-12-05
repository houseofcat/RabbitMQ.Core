using System;

namespace RabbitMQ.Client.Events
{
    ///<summary>
    ///Describes an exception that was thrown during
    ///automatic connection recovery performed by the library.
    ///</summary>
    public class RecoveryExceptionEventArgs : BaseExceptionEventArgs
    {
        public RecoveryExceptionEventArgs(Exception e) : base(e)
        {
        }
    }
}
