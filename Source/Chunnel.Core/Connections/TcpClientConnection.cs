using System.Net.Sockets;
using Chunnel.Core.Interfaces;
using Chunnel.Core.Models;
using Microsoft.Extensions.Logging;

namespace Chunnel.Core.Connections;

public sealed class TcpClientConnection : IConnection, IDisposable
{
  public TcpClientConnection(TcpConfig config, ILogger logger)
  {
    _config = config;
    _logger = logger;
  }

  public void Dispose()
  {
    var socket = _socket;
    _socket = null;

    if (socket is not null)
    {
      Close(socket);
    }
  }

  public async Task<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellation)
  {
    while (cancellation.IsCancellationRequested is false)
    {
      var socket = await GetOrCreateSocketAsync(cancellation).ConfigureAwait(false);
      if (socket is null)
      {
        await Task.Delay(1000, cancellation).ConfigureAwait(false);
        continue;
      }

      using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
      using var cts = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token, cancellation);
      try
      {
        var readed = await socket.ReceiveAsync(buffer, cts.Token).ConfigureAwait(false);
        if (readed == 0)
        {
          _errorsCount++;
          continue;
        }

        return readed;
      }
      catch (OperationCanceledException)
      {
        if (timeout.IsCancellationRequested)
        {
          _errorsCount++;
          continue;
        }

        return 0;
      }
      catch
      {
        Close(socket);
        _socket = null;
      }
    }

    return 0;
  }

  public async Task SendAsync(ReadOnlyMemory<byte> message, CancellationToken cancellation)
  {
    var socket = await GetOrCreateSocketAsync(cancellation).ConfigureAwait(false);
    if (socket is null)
      return;

    try
    {
      var sended = await socket.SendAsync(message, cancellation).ConfigureAwait(false);
      if (sended == 0)
      {
        _errorsCount++;
      }
    }
    catch (OperationCanceledException)
    {
    }
    catch
    {
      Close(socket);
      _socket = null;
    }
  }

  private async Task<Socket?> GetOrCreateSocketAsync(CancellationToken cancellation)
  {
    try
    {
      var socket = _socket;
      if (socket is not null)
      {
        if (_errorsCount < _errorsLimit)
          return socket;

        Close(socket);
        _socket = null;
      }

      _errorsCount = 0;
      socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
      {
        NoDelay = true
      };
      await socket.ConnectAsync(_config.Address, _config.Port, cancellation).ConfigureAwait(false);
      _socket = socket;
      return socket;
    }
    catch
    {
      return null;
    }
  }

  private static void Close(Socket socket)
  {
    try
    {
      socket.Shutdown(SocketShutdown.Both);
    }
    catch
    {
    }

    try
    {
      socket.Close();
    }
    catch
    {
    }

    try
    {
      socket.Dispose();
    }
    catch
    {
    }
  }

  private const int _errorsLimit = 5;

  private Socket? _socket;
  private volatile int _errorsCount;
  private readonly TcpConfig _config;
  private readonly ILogger _logger;
}
