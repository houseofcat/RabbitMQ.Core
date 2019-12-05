namespace RabbitMQ.Client
{
    public class ExternalMechanismFactory : IAuthMechanismFactory
    {
        /// <summary>
        /// The name of the authentication mechanism, as negotiated on the wire.
        /// </summary>
        public string Name
        {
            get { return "EXTERNAL"; }
        }

        /// <summary>
        /// Return a new authentication mechanism implementation.
        /// </summary>
        public IAuthMechanism GetInstance()
        {
            return new ExternalMechanism();
        }
    }
}
