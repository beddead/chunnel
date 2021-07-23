using chunnel.Model.Config;

namespace Chunnel.Model
{
  internal interface IConnectionFactory
  {
    IConnection Create(ConnectionPoint connectionPoint);
  }
}
