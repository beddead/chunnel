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
  internal class TcpClientConnection : TcpConnectionBase
  {
    public TcpClientConnection(TcpConnectionPoint connectionPoint, string name, ILogger logger) : 
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
        while (!cancellation.IsCancellationRequested)
        {
          try
          {
            _logger.LogTrace(string.Format(LocStrings.Connecting, Name));
            if (!_socket.Connected)
              await _socket.ConnectAsync(endPoint, cancellation).ConfigureAwait(false);

            await RunTunnelLoop(_socket, reader, writer, cancellation);
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

            try
            {
              _socket.Disconnect(true);
            }
            catch (Exception ex)
            {
              _logger.LogError(ex.Message);
            }
          }

          await Task.Delay(TimeSpan.FromSeconds(5), cancellation);
        }
      }
      finally
      {
        await CloseAsync(_socket);
      }
    }
  }
}
