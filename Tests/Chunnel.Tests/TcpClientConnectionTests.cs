using Chunnel.Model.Config;
using Chunnel.Model.Connections;
using NUnit.Framework;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Threading.Channels;
using System;
using System.Threading;
using System.Text;

namespace Chunnel.Tests
{
  [TestFixture(TestOf = typeof(TcpClientConnection))]
  [Parallelizable]
  public class TcpConnectionTests
  {
    [Test]
    public async Task LeftToRightTest()
    {
      var serverEndPoint = new TcpConnectionPoint("localhost", 1234, TcpMode.Server);
      var serverConnection = new TcpServerConnection(serverEndPoint, "left",
        GlobalContext.LoggerFactory.CreateLogger<TcpServerConnection>());

      var clientEndPoint = new TcpConnectionPoint("localhost", 1234, TcpMode.Client);
      var clientConnection = new TcpClientConnection(clientEndPoint, "right",
        GlobalContext.LoggerFactory.CreateLogger<TcpClientConnection>());

      var serverChannel = Channel.CreateUnbounded<ReadOnlyMemory<byte>>();
      var clientChannel = Channel.CreateUnbounded<ReadOnlyMemory<byte>>();
      var cancellation = new CancellationTokenSource();

      var serverTask = serverConnection.RunAsync(serverChannel.Reader, clientChannel.Writer, cancellation.Token);
      var clientTask = clientConnection.RunAsync(clientChannel.Reader, serverChannel.Writer, cancellation.Token);

      await WriteStringAsync("writen to server channel", serverChannel.Writer, cancellation.Token);
      await Task.Delay(15000);
      var msg = clientChannel.Reader.ReadAsync(cancellation.Token);
    }

    [Test]
    public async Task WriteToEndPointTest()
    {
      var endPoint = new TcpConnectionPoint("opo-tfs.zav.mir", 443, TcpMode.Client);
      var connection = new TcpClientConnection(endPoint, "http",
        GlobalContext.LoggerFactory.CreateLogger<TcpClientConnection>());

      var left = Channel.CreateUnbounded<ReadOnlyMemory<byte>>();
      var right = Channel.CreateUnbounded<ReadOnlyMemory<byte>>();
      var cancellation = new CancellationTokenSource();

      var task = connection.RunAsync(left.Reader, right.Writer, cancellation.Token);

      await WriteStringAsync("get", left.Writer, cancellation.Token);
      await Task.Delay(15000);
      var answer = right.Reader.ReadAsync();
    }

    private static ValueTask WriteStringAsync(string msg, ChannelWriter<ReadOnlyMemory<byte>> writer, CancellationToken cancellation)
    {
      var buffer = Encoding.UTF8.GetBytes(msg);
      return writer.WriteAsync(buffer.AsMemory(), cancellation);
    }
  }
}