using System.Buffers;

namespace Chunnel.Core.Models;

/// <summary>
/// Буфер не менее указанного размера. Берется из пула массивов
/// </summary>
internal sealed class PooledBuffer : IDisposable
{
  public PooledBuffer(int size)
  {
    _array = ArrayPool<byte>.Shared.Rent(size);
  }

  public void Dispose()
  {
    Dispose(disposing: true);
    GC.SuppressFinalize(this);
  }

  public ReadOnlyMemory<byte> Data => _array.AsMemory(0, DataSize);

  public int DataSize { get; private set; }

  internal void CopyFrom(Memory<byte> memBuffer, int readedBytes)
  {
    memBuffer.Span[..readedBytes].CopyTo(_array.AsSpan());
    DataSize = readedBytes;
  }

  private void Dispose(bool disposing)
  {
    if (!_disposed)
    {
      if (disposing)
      {
        ArrayPool<byte>.Shared.Return(_array);
      }

      _disposed = true;
    }
  }

  private bool _disposed;
  private readonly byte[] _array;
}
