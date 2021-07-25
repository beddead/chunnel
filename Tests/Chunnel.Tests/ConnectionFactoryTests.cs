using Chunnel.Model.Config;
using Chunnel.Model.Connections;
using NUnit.Framework;

namespace Chunnel.Tests
{
  [TestFixture(TestOf = typeof(ConnectionFactory))]
  [Parallelizable]
  public class ConnectionFactoryTests
  {
    [SetUp]
    public void Setup()
    {
      _factory = new ConnectionFactory(GlobalContext.LoggerFactory);
    }
    
    [Test]
    public void CreateTcpClientConnectionTest()
    {
      var connectionPoint = new TcpConnectionPoint("opo-tfs.zav.mir", 443, TcpMode.Client);
      var connection = _factory.Create(connectionPoint, "Left");

      Assert.AreEqual("Left", connection.Name);
      Assert.IsInstanceOf<TcpClientConnection>(connection);
    }

    private ConnectionFactory _factory;
  }
}