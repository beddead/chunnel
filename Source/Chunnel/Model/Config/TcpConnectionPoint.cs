using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Chunnel.Model.Config
{
  internal class TcpConnectionPoint : ConnectionPoint
  {
    public string Address { get; }

    public TcpConnectionPoint(string address, int port, TcpMode mode)
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
