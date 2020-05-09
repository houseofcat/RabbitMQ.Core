using System.Text;

namespace RabbitMQ.Client
{
    public class PlainMechanism : IAuthMechanism
    {
        public byte[] handleChallenge(byte[] challenge, IConnectionFactory factory)
        {
            return Encoding.UTF8.GetBytes("\0" + factory.UserName + "\0" + factory.Password);
        }
    }
}
