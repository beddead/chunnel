using System.IO.Ports;
using Chunnel.Core.Interfaces;
using Chunnel.Core.Models;

namespace Chunnel.Core.Connections;

public sealed class SerialPortConnection : IConnection, IDisposable
{
  public SerialPortConnection(SerialPortConfig config)
  {
    _config = config;
  }

  public void Dispose()
  {
    var serialPort = _serialPort;
    if (serialPort is not null)
      Close(serialPort);
    _serialPort = null;
  }

  public async Task<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellation)
  {
    var serialPort = GetOrCreatePort();
    if (serialPort is not null)
    {
      try
      {
        while (cancellation.IsCancellationRequested is false)
        {
          var readed = await serialPort.BaseStream.ReadAsync(buffer, cancellation).ConfigureAwait(false);
          if (readed != 0)
            return readed;

          await Task.Delay(30, cancellation).ConfigureAwait(false);
        }
      }
      catch
      {
        _serialPort = null;
        Close(serialPort);
      }
    }

    return 0;
  }

  public async Task SendAsync(ReadOnlyMemory<byte> message, CancellationToken cancellation)
  {
    var serialPort = GetOrCreatePort();
    if (serialPort is not null)
    {
      try
      {
        await serialPort.BaseStream.WriteAsync(message, cancellation).ConfigureAwait(false);
      }
      catch
      {
      }
    }
  }

  private SerialPort? GetOrCreatePort()
  {
    var serialPort = _serialPort;
    if (serialPort is not null)
    {
      if (CheckSerialPort(serialPort))
        return serialPort;
      Close(serialPort);
      _serialPort = null;
    }

    serialPort = Create();
    _serialPort = serialPort;
    return serialPort;
  }

  private SerialPort? Create()
  {
    try
    {
      var serialPort = new SerialPort(_config.PortName, _config.BaudRate, _config.Parity, _config.DataBits, _config.StopBits);
      serialPort.Open();
      return serialPort;
    }
    catch
    {
      return null;
    }
  }

  private static void Close(SerialPort serialPort)
  {
    try
    {
      serialPort.Close();
    }
    catch
    {
    }

    serialPort.Dispose();
  }

  private static bool CheckSerialPort(SerialPort serialPort)
  {
    try
    {
      return serialPort.IsOpen;
    }
    catch
    {
      return false;
    }
  }

  private readonly SerialPortConfig _config;
  private SerialPort? _serialPort;
}
