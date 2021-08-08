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
    /// <summary>
    /// Run connection processing loop
    /// </summary>
    /// <param name="reader">Reader from connection will read messages and send it to connection end point</param>
    /// <param name="writer">Writer connection will write recieved message from connection end point</param>
    /// <param name="cancellation"></param>
    /// <returns></returns>
    Task RunAsync(ChannelReader<ReadOnlyMemory<byte>> reader,
      ChannelWriter<ReadOnlyMemory<byte>> writer, CancellationToken cancellation);

    /// <summary>
    /// Connection name
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Log recieved and sended messages
    /// </summary>
    bool LogData { get; set; }
  }
}
