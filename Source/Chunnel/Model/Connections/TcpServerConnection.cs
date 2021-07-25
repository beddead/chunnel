using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Chunnel.Model.Config;
using Microsoft.Extensions.Logging;

namespace Chunnel.Model.Connections
{
  internal class TcpServerConnection : IConnection
  {
    public TcpServerConnection(TcpConnectionPoint connectionPoint, string name, ILogger logger)
    {
      _connectionPoint = connectionPoint ?? throw new ArgumentNullException(nameof(connectionPoint));
      _logger = logger ?? throw new ArgumentNullException(nameof(logger));
      Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    public Task RunAsync(Channel<ReadOnlyMemory<byte>> channel, CancellationToken cancellation)
    {
      throw new NotImplementedException();
    }

    public Channel<ReadOnlyMemory<byte>> Channel
    {
      get
      {
        throw new NotImplementedException();
      }
    }

    public string Name { get; }

    private readonly TcpConnectionPoint _connectionPoint;
    private readonly ILogger _logger;
  }
}
