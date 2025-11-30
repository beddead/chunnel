namespace Chunnel.Core;

public interface IConnectedEndpoint
{
  Task SendAsync(ReadOnlyMemory<byte> message, CancellationToken cancellation);

  Task<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellation);
}
