using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Chunnel.Model
{
  internal interface IConnection
  {
    Task OpenAsync();

    Task CloseAsync();

    Channel<ReadOnlyMemory<byte>> Channel { get; }
  }
}
