using NUnit.Framework;
using RabbitMQ.Client.Impl;
using System.Net;
using System.Net.Sockets;

namespace RabbitMQ.Client.Unit
{
    [TestFixture]
    public class TestTcpClientAdapter
    {
        [Test]
        public void TcpClientAdapterHelperGetMatchingHostReturnNoAddressIfFamilyDoesNotMatch()
        {
            var address = IPAddress.Parse("127.0.0.1");
            IPAddress matchingAddress = TcpClientAdapterHelper.GetMatchingHost(new[] { address }, AddressFamily.InterNetworkV6);
            Assert.IsNull(matchingAddress);
        }

        [Test]
        public void TcpClientAdapterHelperGetMatchingHostReturnsSingleAddressIfFamilyIsUnspecified()
        {
            var address = IPAddress.Parse("1.1.1.1");
            IPAddress matchingAddress = TcpClientAdapterHelper.GetMatchingHost(new[] { address }, AddressFamily.Unspecified);
            Assert.AreEqual(address, matchingAddress);
        }

        [Test]
        public void TcpClientAdapterHelperGetMatchingHostReturnNoAddressIfFamilyIsUnspecifiedAndThereIsNoSingleMatch()
        {
            var address = IPAddress.Parse("1.1.1.1");
            var address2 = IPAddress.Parse("2.2.2.2");
            IPAddress matchingAddress = TcpClientAdapterHelper.GetMatchingHost(new[] { address, address2 }, AddressFamily.Unspecified);
            Assert.IsNull(matchingAddress);
        }
    }
}
