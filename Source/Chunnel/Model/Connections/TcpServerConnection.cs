using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Chunnel.Model.Config;
using Microsoft.Extensions.Logging;
using LocStrings = Chunnel.Properties.Resources;

namespace Chunnel.Model.Connections
{
  internal class TcpServerConnection : TcpConnectionBase
  {
    public TcpServerConnection(TcpConnectionPoint connectionPoint, string name, ILogger logger) :
      base(connectionPoint, name, logger)
    {
    }

    public override async Task RunAsync(ChannelReader<ReadOnlyMemory<byte>> reader,
      ChannelWriter<ReadOnlyMemory<byte>> writer, CancellationToken cancellation)
    {
      var endPoint = Setup();

      cancellation.ThrowIfCancellationRequested();

      try
      {
        _logger.LogTrace(string.Format(LocStrings.Listening, Name));
        _socket.Bind(endPoint);
        _socket.Listen(100);

        while (!cancellation.IsCancellationRequested)
        {
          try
          {
            var socket = await _socket.AcceptAsync();
            _ = RunTunnelLoop(socket, reader, writer, cancellation);
          }
          catch (TaskCanceledException)
          {
            return;
          }
          catch (AggregateException e) when (e.InnerException is TaskCanceledException)
          {
            return;
          }
          catch (Exception e)
          {
            _logger.LogError(e, LocStrings.ConnectionLoopError);
            await Task.Delay(TimeSpan.FromSeconds(5), cancellation);
          }
        }
      }
      finally
      {
        await CloseAsync(_socket);
      }
    }
  }
}
