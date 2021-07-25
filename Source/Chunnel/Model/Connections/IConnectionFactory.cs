using Chunnel.Model.Config;

namespace Chunnel.Model.Connections
{
  internal interface IConnectionFactory
  {
    IConnection Create(ConnectionPoint connectionPoint, string name);
  }
}
