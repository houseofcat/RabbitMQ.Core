namespace RabbitMQ.Client
{
    public class ExternalMechanism : IAuthMechanism
    {
        /// <summary>
        /// Handle one round of challenge-response.
        /// </summary>
        public byte[] handleChallenge(byte[] challenge, IConnectionFactory factory)
        {
            return new byte[0];
        }
    }
}
