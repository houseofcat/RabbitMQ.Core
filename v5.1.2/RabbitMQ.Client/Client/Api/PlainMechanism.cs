using System.Text;

namespace RabbitMQ.Client
{
    public class PlainMechanism : AuthMechanism
    {
        public byte[] HandleChallenge(byte[] challenge, IConnectionFactory factory)
        {
            return Encoding.UTF8.GetBytes("\0" + factory.UserName + "\0" + factory.Password);
        }
    }
}
