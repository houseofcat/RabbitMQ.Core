namespace RabbitMQ.Client
{
    public interface IAuthMechanismFactory
    {
        /// <summary>
        /// The name of the authentication mechanism, as negotiated on the wire.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Return a new authentication mechanism implementation.
        /// </summary>
        IAuthMechanism GetInstance();
    }
}
