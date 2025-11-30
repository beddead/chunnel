namespace Chunnel.Core.Interfaces;

public interface IConnection
{
  Task SendAsync(ReadOnlyMemory<byte> message, CancellationToken cancellation);

  Task<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellation);
}
