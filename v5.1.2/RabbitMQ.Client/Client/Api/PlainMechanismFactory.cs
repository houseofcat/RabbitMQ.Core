namespace RabbitMQ.Client
{
    public class PlainMechanismFactory : IAuthMechanismFactory
    {
        /// <summary>
        /// The name of the authentication mechanism, as negotiated on the wire.
        /// </summary>
        public string Name
        {
            get { return "PLAIN"; }
        }

        /// <summary>
        /// Return a new authentication mechanism implementation.
        /// </summary>
        public IAuthMechanism GetInstance()
        {
            return new PlainMechanism();
        }
    }
}
