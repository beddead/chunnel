using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Chunnel.Model.Args;
using Chunnel.Model.Config;
using Chunnel.Model.Connections;
using Microsoft.Extensions.Logging;

namespace Chunnel.Model
{
  class Runner
  {
    public Runner(Options options, ILoggerFactory loggerFactory)
    {
      _options = options ?? throw new ArgumentNullException(nameof(options));
      _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
      _logger = _loggerFactory.CreateLogger<Runner>();
    }

    public Task RunAsync(CancellationToken cancellation)
    {
      var config = CreateConfig();
      CreateConnections(config, out var leftConnection, out var rightConnection);
      return StartConnectionsAsync(leftConnection, rightConnection, cancellation);
    }

    private async Task StartConnectionsAsync(IConnection leftConnection, IConnection rightConnection, CancellationToken cancellation)
    {
      _logger.LogInformation(Properties.Resources.StartingConnectionsAndWorkingLoop);

      CreateBuffers(out var leftBuffer, out var rightBuffer);
      try
      {
        leftConnection.LogData = true;
        var left = leftConnection.RunAsync(leftBuffer.Reader, rightBuffer.Writer, cancellation);
        var right = rightConnection.RunAsync(rightBuffer.Reader, leftBuffer.Writer, cancellation);
        await Task.WhenAny(left, right);
        left.Wait(cancellation);
        right.Wait(cancellation);
      }
      catch (Exception e)
      {
        _logger.LogError(e, Properties.Resources.WorkingLoopFail);
        throw;
      }
    }

    private void CreateConnections(TunnelConfig config, out IConnection leftConnection, out IConnection rightConnection)
    {
      _logger.LogInformation(Properties.Resources.CreatingConnections);
      try
      {
        var factory = new ConnectionFactory(_loggerFactory);
        leftConnection = factory.Create(config.Left, "Left");
        rightConnection = factory.Create(config.Right, "Right");
      }
      catch (Exception e)
      {
        _logger.LogError(e, Properties.Resources.CreatingConnectionsFailed);
        throw;
      }
    }

    private static void CreateBuffers(out Channel<ReadOnlyMemory<byte>> leftBuffer, out Channel<ReadOnlyMemory<byte>> rightBuffer)
    {
      var options = new BoundedChannelOptions(1);
      options.FullMode = BoundedChannelFullMode.DropOldest;

      leftBuffer = Channel.CreateBounded<ReadOnlyMemory<byte>>(options);
      rightBuffer = Channel.CreateBounded<ReadOnlyMemory<byte>>(options);
    }

    private TunnelConfig CreateConfig()
    {
      _logger.LogInformation(Properties.Resources.CreatingConnectionsConfig);
      try
      {
        var config = new TunnelConfig(_options.GetLeft(), _options.GetRight());
        _logger.LogInformation(Properties.Resources.LeftConnectionConfig + ":");
        _logger.LogInformation(config.Left.ToString());

        _logger.LogInformation(Properties.Resources.RightConnectionConfig + ":");
        _logger.LogInformation(config.Right.ToString());

        return config;

      }
      catch (Exception e)
      {
        _logger.LogError(e, Properties.Resources.CreatingConnectionsConfigFailed);
        throw;
      }
    }

    private readonly Options _options;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<Runner> _logger;
  }
}
