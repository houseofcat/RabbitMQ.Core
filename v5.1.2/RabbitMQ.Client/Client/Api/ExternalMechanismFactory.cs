namespace RabbitMQ.Client
{
    public class ExternalMechanismFactory : AuthMechanismFactory
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
        public AuthMechanism GetInstance()
        {
            return new ExternalMechanism();
        }
    }
}
