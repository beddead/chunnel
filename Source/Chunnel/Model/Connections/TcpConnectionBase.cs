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
  internal abstract class TcpConnectionBase : IConnection
  {
    static TcpConnectionBase()
    {
      _poolPolicy = new StringBuilderPooledObjectPolicy();
      _stringBuilderPool = new DefaultObjectPool<StringBuilder>(_poolPolicy);
    }

    public TcpConnectionBase(TcpConnectionPoint connectionPoint, string name, ILogger logger)
    {
      _connectionPoint = connectionPoint ?? throw new ArgumentNullException(nameof(connectionPoint));
      Name = name ?? throw new ArgumentNullException(nameof(name));
      _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public abstract Task RunAsync(ChannelReader<ReadOnlyMemory<byte>> reader, ChannelWriter<ReadOnlyMemory<byte>> writer, CancellationToken cancellation);

    public string Name { get; }

    public bool LogData { get; set; }

    protected IPEndPoint Setup(ChannelReader<ReadOnlyMemory<byte>> reader, ChannelWriter<ReadOnlyMemory<byte>> writer)
    {
      _reader = reader ?? throw new ArgumentNullException(nameof(reader));
      _writer = writer ?? throw new ArgumentNullException(nameof(writer));

      if (!IPAddress.TryParse(_connectionPoint.Address, out var ipAddress))
      {
        var hostEntry = Dns.GetHostEntry(_connectionPoint.Address);
        ipAddress = hostEntry.AddressList[0];
      }
      var endPoint = new IPEndPoint(ipAddress, _connectionPoint.Port);

      _socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
      {
        NoDelay = true,
        ReceiveBufferSize = _bufferSize,
        SendBufferSize = _bufferSize
      };

      _readBuffer = new byte[_bufferSize];
      return endPoint;
    }

    protected async Task RunTunnelLoop(Socket socket, CancellationToken cancellation)
    {
      _logger.LogTrace(string.Format(LocStrings.StartingWorkLoop, Name, socket.LocalEndPoint, 
        socket.RemoteEndPoint));
      var readLoop = ReadLoopAsync(socket, cancellation);
      var writeLoop = WriteLoopAsync(socket, cancellation);
      await Task.WhenAll(readLoop, writeLoop);
    }

    protected async Task ReadLoopAsync(Socket socket, CancellationToken cancellation)
    {
      var buffer = new Memory<byte>(_readBuffer);

      while (!cancellation.IsCancellationRequested)
      {
        var readed = await socket.ReceiveAsync(buffer, SocketFlags.None, cancellation).ConfigureAwait(false);
        if (readed != 0)
        {
          if (LogData)
          {
            _logger.LogTrace(string.Format(LocStrings.RecievedBytesFromConnection, Name, readed,
              BufferToString(buffer, readed)));
          }
          await WriteToChannelAsync(buffer, readed, cancellation);
        }
      }
    }

    protected async Task WriteLoopAsync(Socket socket, CancellationToken cancellation)
    {
      await foreach (var msg in _reader.ReadAllAsync(cancellation))
      {
        if (cancellation.IsCancellationRequested)
          return;

        if (LogData)
        {
          _logger.LogTrace(string.Format(LocStrings.SendingBytesToConnection, Name, msg.Length,
            BufferToString(msg, msg.Length)));
        }
        await socket.SendAsync(msg, SocketFlags.None, cancellation).ConfigureAwait(false);
      }
    }

    protected ValueTask WriteToChannelAsync(Memory<byte> buffer, int readed, CancellationToken cancellation)
    {
      var outBuffer = new Memory<byte>(buffer.Slice(0, readed).ToArray());
      return _writer.WriteAsync(outBuffer, cancellation);
    }

    protected static Task CloseAsync(Socket socket)
    {
      if (socket.Connected)
        socket.Close();

      return Task.CompletedTask;
    }

    protected static string BufferToString(ReadOnlyMemory<byte> buffer, int readed)
    {
      var builder = _stringBuilderPool.Get();
      var span = buffer.Span;
      for (int i = 0; i < readed; i++)
      {
        if (i == 0)
          builder.Append(span[i].ToString("X2"));
        else
          builder.Append(" " + span[i].ToString("X2"));
      }

      var s = builder.ToString();
      _stringBuilderPool.Return(builder);

      return s;
    }

    protected readonly TcpConnectionPoint _connectionPoint;
    protected readonly ILogger _logger;
    protected Socket _socket;
    protected byte[] _readBuffer;
    protected static readonly StringBuilderPooledObjectPolicy _poolPolicy;
    protected static readonly ObjectPool<StringBuilder> _stringBuilderPool;
    protected ChannelReader<ReadOnlyMemory<byte>> _reader;
    protected ChannelWriter<ReadOnlyMemory<byte>> _writer;

    private const int _bufferSize = 60000;
  }
}
