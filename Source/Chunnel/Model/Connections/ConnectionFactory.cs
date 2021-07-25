using System;
using Chunnel.Model.Config;
using Microsoft.Extensions.Logging;

namespace Chunnel.Model.Connections
{
  internal class ConnectionFactory : IConnectionFactory
  {
    public ConnectionFactory(ILoggerFactory loggerFactory)
    {
      _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    }
    
    public IConnection Create(ConnectionPoint connectionPoint, string name)
    {
      switch (connectionPoint)
      {
        case TcpConnectionPoint tcp:
          return tcp.Mode == TcpMode.Client ? CreateTcpClient(tcp, name) : CreateTcpServer(tcp, name);

        default:
          throw new ArgumentException();
      }
    }

    private TcpServerConnection CreateTcpServer(TcpConnectionPoint connectionPoint, string name)
    {
      return new TcpServerConnection(connectionPoint, name, _loggerFactory.CreateLogger<TcpServerConnection>());
    }

    private TcpClientConnection CreateTcpClient(TcpConnectionPoint connectionPoint, string name)
    {
      return new TcpClientConnection(connectionPoint, name, _loggerFactory.CreateLogger<TcpServerConnection>());
    }

    private readonly ILoggerFactory _loggerFactory;
  }
}
