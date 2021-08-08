using System;

namespace Chunnel.Model.Config
{
  internal class TunnelConfig
  {
    public TunnelConfig(ConnectionPoint left, ConnectionPoint right)
    {
      Left = left ?? throw new ArgumentNullException(nameof(left));
      Right = right ?? throw new ArgumentNullException(nameof(right));
    }

    public ConnectionPoint Left { get; }

    public ConnectionPoint Right { get; }
  }
}
