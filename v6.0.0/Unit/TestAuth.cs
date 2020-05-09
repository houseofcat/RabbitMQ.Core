using NUnit.Framework;
using RabbitMQ.Client.Exceptions;

namespace RabbitMQ.Client.Unit
{
    [TestFixture]
    public class TestAuth
    {
        [Test]
        public void TestAuthFailure()
        {
            ConnectionFactory connFactory = new ConnectionFactory
            {
                UserName = "guest",
                Password = "incorrect-password"
            };

            try
            {
                connFactory.CreateConnection();
                Assert.Fail("Exception caused by authentication failure expected");
            }
            catch (BrokerUnreachableException bue)
            {
                Assert.IsInstanceOf<AuthenticationFailureException>(bue.InnerException);
            }
        }
    }
}

