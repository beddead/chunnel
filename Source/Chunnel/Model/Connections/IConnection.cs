using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Chunnel.Model.Connections
{
  internal interface IConnection
  {
    Task RunAsync(Channel<ReadOnlyMemory<byte>> channel, CancellationToken cancellation);

    string Name { get; }
  }
}
