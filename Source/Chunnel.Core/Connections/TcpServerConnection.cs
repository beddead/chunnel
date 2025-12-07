using System.Net;
using System.Net.Sockets;
using Chunnel.Core.Interfaces;
using Chunnel.Core.Models;
using Microsoft.Extensions.Logging;

namespace Chunnel.Core.Connections;

public sealed class TcpServerConnection : IConnection, IDisposable
{
  public TcpServerConnection(TcpConfig config, ILogger logger)
  {
    _config = config;
    _logger = logger;
    _stopCts = new CancellationTokenSource();
  }

  public void Dispose()
  {
    _stopCts.Cancel();
    _stopCts.Dispose();

    throw new NotImplementedException();
  }

  public Task<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellation)
  {
    throw new NotImplementedException();
  }

  public Task SendAsync(ReadOnlyMemory<byte> message, CancellationToken cancellation)
  {
    throw new NotImplementedException();
  }

  private Socket? GetOrCreateServerSocketAsync(CancellationToken cancellation)
  {
    try
    {
      var socket = _serverSocket;
      if (socket is not null)
        return socket;

      socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
      {
        NoDelay = true
      };
      socket.Bind(new IPEndPoint(IPAddress.Parse(_config.Address), _config.Port));
      socket.Listen();
      _ = Task.Run(() => AcceptConnectionsAsync(socket, _stopCts.Token), _stopCts.Token);

      return socket;
    }
    catch (OperationCanceledException)
    {
      return null;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Сбой при создании серверного сокета для {IpAddress}:{Port}", _config.Address, _config.Port);
      return null;
    }
  }

  private async Task AcceptConnectionsAsync(Socket socket, CancellationToken stopToken)
  {
    using var stopProcessingCts = new CancellationTokenSource();
    using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(stopToken, stopProcessingCts.Token);
    try
    {
      while (stopToken.IsCancellationRequested is false)
      {
        try
        {
          var incomingSocket = await socket.AcceptAsync(stopToken).ConfigureAwait(false);
          var remoteEp = (IPEndPoint) incomingSocket.RemoteEndPoint!;
          _logger.LogInformation("Новое входящее соединение для {IpAddress}:{Port} от {RemoteAddress}:{RemotePort}",
            _config.Address, _config.Port, remoteEp.Address, remoteEp.Port);

          _ = Task.Run(() => ReadFromSocketAsync(incomingSocket, combinedCts.Token));
        }
        catch (OperationCanceledException)
        {
          _logger.LogDebug("Отмена ожидания входящих соединений для {IpAddress}:{Port}", _config.Address, _config.Port);
          return;
        }
        catch (Exception ex)
        {
          _logger.LogError(ex, "Сбой при ожидании входящего соединения для {IpAddress}:{Port}", _config.Address, _config.Port);
        }
      }
    }
    finally
    {
      stopProcessingCts.Cancel();
    }
  }

  private const int _errorsLimit = 5;

  private Socket? _serverSocket;

  private readonly TcpConfig _config;
  private readonly ILogger _logger;
  private readonly CancellationTokenSource _stopCts;
}
