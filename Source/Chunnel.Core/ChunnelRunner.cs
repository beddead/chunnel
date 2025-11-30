using System.Threading.Channels;
using Chunnel.Core.Interfaces;
using Chunnel.Core.Models;

namespace Chunnel.Core;

public class ChunnelRunner
{
  public ChunnelRunner(IConnection leftEndpoint, IConnection rightEndpoint)
  {
    _leftEndpoint = leftEndpoint;
    _rightEndpoint = rightEndpoint;
  }

  public async Task RunAsync(CancellationToken stopToken)
  {
    var left2Right = Channel.CreateBounded<PooledBuffer>(new BoundedChannelOptions(100)
    {
      FullMode = BoundedChannelFullMode.Wait,
      SingleReader = true,
      SingleWriter = false,
      AllowSynchronousContinuations = true
    });

    var right2Left = Channel.CreateBounded<PooledBuffer>(new BoundedChannelOptions(100)
    {
      FullMode = BoundedChannelFullMode.Wait,
      SingleReader = true,
      SingleWriter = false,
      AllowSynchronousContinuations = true
    });

    var context = new ChunnelContext(left2Right, right2Left);

    await RunInternalAsync(context, stopToken).ConfigureAwait(false);
  }

  private async Task RunInternalAsync(ChunnelContext context, CancellationToken stopToken)
  {
    Task[] tasks = [
      ReadLeftAsync(context.Left2Right.Writer, stopToken),
      WriteToRightAsync(context.Left2Right.Reader, stopToken),
      ReadRightAsync(context.Right2Left.Writer, stopToken),
      WriteToLeftAsync(context.Right2Left.Reader, stopToken)
    ];

    foreach (var task in tasks)
    {
      try
      {
        await task.ConfigureAwait(false);
      }
      catch (OperationCanceledException)
      {
      }
    }
  }

  /// <summary>
  /// Записывает данные из левого соединения в правое соединение
  /// </summary>
  private async Task WriteToRightAsync(ChannelReader<PooledBuffer> reader, CancellationToken stopToken)
  {
    try
    {
      await foreach (var dataFromLeft in reader.ReadAllAsync(stopToken).ConfigureAwait(false))
      {
        try
        {
          await _rightEndpoint.SendAsync(dataFromLeft.Data, stopToken).ConfigureAwait(false);
        }
        finally
        {
          dataFromLeft.Dispose();
        }
      }
    }
    catch (OperationCanceledException)
    {
    }
  }

  /// <summary>
  /// Читает данные из левого соединения, и передает их для записи в правое соединение
  /// </summary>
  private async Task ReadLeftAsync(ChannelWriter<PooledBuffer> toRightWriter, CancellationToken stopToken)
  {
    try
    {
      var buffer = new byte[_bufferSize];
      var memBuffer = buffer.AsMemory();
      while (stopToken.IsCancellationRequested is false)
      {
        var readedBytes = await _leftEndpoint.ReadAsync(memBuffer, stopToken).ConfigureAwait(false);
        if (readedBytes == 0)
          continue;

        var bufferToRight = new PooledBuffer(buffer.Length);
        bufferToRight.CopyFrom(memBuffer, readedBytes);
        try
        {
          await toRightWriter.WriteAsync(bufferToRight, stopToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
          bufferToRight.Dispose();
        }
      }
    }
    catch (OperationCanceledException)
    {
    }
  }

  /// <summary>
  /// Записывает данные из правого соединения в левое соединение
  /// </summary>
  private async Task WriteToLeftAsync(ChannelReader<PooledBuffer> reader, CancellationToken stopToken)
  {
    try
    {
      await foreach (var dataFromRight in reader.ReadAllAsync(stopToken).ConfigureAwait(false))
      {
        try
        {
          await _leftEndpoint.SendAsync(dataFromRight.Data, stopToken).ConfigureAwait(false);
        }
        finally
        {
          dataFromRight.Dispose();
        }
      }
    }
    catch (OperationCanceledException)
    {
    }
  }

  /// <summary>
  /// Читает данные из правого соединения, и передает их для записи в левое соединение
  /// </summary>
  private async Task ReadRightAsync(ChannelWriter<PooledBuffer> toLeftWriter, CancellationToken stopToken)
  {
    try
    {
      var buffer = new byte[_bufferSize];
      var memBuffer = buffer.AsMemory();
      while (stopToken.IsCancellationRequested is false)
      {
        var readedBytes = await _rightEndpoint.ReadAsync(memBuffer, stopToken).ConfigureAwait(false);
        if (readedBytes == 0)
          continue;

        var bufferToLeft = new PooledBuffer(buffer.Length);
        bufferToLeft.CopyFrom(memBuffer, readedBytes);
        try
        {
          await toLeftWriter.WriteAsync(bufferToLeft, stopToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
          bufferToLeft.Dispose();
        }
      }
    }
    catch (OperationCanceledException)
    {
    }
  }

  private const int _bufferSize = 2048;

  private readonly IConnection _leftEndpoint;
  private readonly IConnection _rightEndpoint;
}
