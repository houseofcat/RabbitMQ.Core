namespace RabbitMQ.Client
{
    public class ExternalMechanism : AuthMechanism
    {
        /// <summary>
        /// Handle one round of challenge-response.
        /// </summary>
        public byte[] HandleChallenge(byte[] challenge, IConnectionFactory factory)
        {
            return new byte[0];
        }
    }
}
