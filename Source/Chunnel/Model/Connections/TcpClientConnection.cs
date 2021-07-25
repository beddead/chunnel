using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Chunnel.Model.Config;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using LocStrings = Chunnel.Properties.Resources;

namespace Chunnel.Model.Connections
{
  internal class TcpClientConnection : IConnection
  {
    public TcpClientConnection(TcpConnectionPoint connectionPoint, string name, ILogger logger)
    {
      _connectionPoint = connectionPoint ?? throw new ArgumentNullException(nameof(connectionPoint));
      Name = name ?? throw new ArgumentNullException(nameof(name));
      _logger = logger ?? throw new ArgumentNullException(nameof(logger));
      _poolPolicy = new StringBuilderPooledObjectPolicy();
      _stringBuilderPool = new DefaultObjectPool<StringBuilder>(_poolPolicy);
    }

    public async Task RunAsync(Channel<ReadOnlyMemory<byte>> channel, CancellationToken cancellation)
    {
      var endPoint = Setup(channel);

      cancellation.ThrowIfCancellationRequested();

      try
      {
        while (!cancellation.IsCancellationRequested)
        {
          try
          {
            _logger.LogTrace(string.Format(LocStrings.Connecting, Name));
            await _socket.ConnectAsync(endPoint, cancellation).ConfigureAwait(false);

            await RunTunnelLoop(cancellation);
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
          }

          await Task.Delay(TimeSpan.FromSeconds(5), cancellation);
        }
      }
      finally
      {
        await CloseAsync();
      }
    }

    private IPEndPoint Setup(Channel<ReadOnlyMemory<byte>> channel)
    {
      _channel = channel ?? throw new ArgumentNullException(nameof(channel));

      var hostEntry = Dns.GetHostEntry(_connectionPoint.Address);
      var endPoint = new IPEndPoint(hostEntry.AddressList[0], _connectionPoint.Port);

      _socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
      {
        NoDelay = true,
        ReceiveBufferSize = _bufferSize,
        SendBufferSize = _bufferSize
      };

      _readBuffer = new byte[_bufferSize];
      return endPoint;
    }

    public string Name { get; }

    private async Task RunTunnelLoop(CancellationToken cancellation)
    {
      _logger.LogTrace(string.Format(LocStrings.StartingWorkLoop, Name));
      var readLoop = ReadLoopAsync(cancellation);
      var writeLoop = WriteLoopAsync(cancellation);
      await Task.WhenAll(readLoop, writeLoop);
    }

    private async Task ReadLoopAsync(CancellationToken cancellation)
    {
      var buffer = new Memory<byte>(_readBuffer);

      while (!cancellation.IsCancellationRequested)
      {
        var readed = await _socket.ReceiveAsync(buffer, SocketFlags.None, cancellation).ConfigureAwait(false);
        if (readed != 0)
        {
          _logger.LogTrace(string.Format(LocStrings.RecievedBytesFromConnection, Name, readed,
            BufferToString(buffer, readed)));
          await WriteToChannelAsync(buffer, readed, cancellation);
        }
      }
    }

    private async Task WriteLoopAsync(CancellationToken cancellation)
    {
      await foreach (var msg in _channel.Reader.ReadAllAsync(cancellation))
      {
        if (cancellation.IsCancellationRequested)
          return;

        await _socket.SendAsync(msg, SocketFlags.None, cancellation).ConfigureAwait(false);
      }
    }

    private ValueTask WriteToChannelAsync(Memory<byte> buffer, int readed, CancellationToken cancellation)
    {
      var outBuffer = new Memory<byte>(buffer.ToArray());
      return _channel.Writer.WriteAsync(outBuffer, cancellation);
    }

    private string BufferToString(Memory<byte> buffer, int readed)
    {
      var builder = _stringBuilderPool.Get();
      var span = buffer.Span;
      for (int i = 0; i < readed; i++)
      {
        if (i == 0)
          builder.Append(span[i].ToString("X2"));
        else
          builder.Append(span[i].ToString(" X2"));
      }

      var s = builder.ToString();
      _stringBuilderPool.Return(builder);

      return s;
    }

    private Task CloseAsync()
    {
      if (_socket.Connected)
        _socket.Close();

      return Task.CompletedTask;
    }

    private const int _bufferSize = 60000;

    private readonly TcpConnectionPoint _connectionPoint;
    private readonly ILogger _logger;
    private Socket _socket;
    private byte[] _readBuffer;
    private readonly StringBuilderPooledObjectPolicy _poolPolicy;
    private readonly ObjectPool<StringBuilder> _stringBuilderPool;
    private Channel<ReadOnlyMemory<byte>> _channel;
  }
}
