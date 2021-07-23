using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace chunnel.Model.Config
{
  internal class TcpConnectionPoint : ConnectionPoint
  {
    public IPAddress Address { get; }

    public TcpConnectionPoint(IPAddress address, int port, TcpMode mode)
    {
      Address = address ?? throw new ArgumentNullException(nameof(address));
      Port = port;
      Mode = mode;
    }

    public int Port { get; }

    /// <summary>
    /// Connection mode
    /// </summary>
    public TcpMode Mode { get; }
  }
}
