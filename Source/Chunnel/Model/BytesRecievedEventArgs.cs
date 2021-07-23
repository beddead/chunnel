using System;

namespace Chunnel.Model
{
  internal class BytesRecievedEventArgs : EventArgs
  {
    public BytesRecievedEventArgs(in ReadOnlyMemory<byte> buffer)
    {
      Buffer = buffer;
    }

    public readonly ReadOnlyMemory<byte> Buffer;
  }
}
