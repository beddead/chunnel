using System.Threading.Channels;
using Chunnel.Core.Models;

namespace Chunnel.Core;

internal sealed record ChunnelContext(Channel<PooledBuffer> Left2Right, Channel<PooledBuffer> Right2Left);
