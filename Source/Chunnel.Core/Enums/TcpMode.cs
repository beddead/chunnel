namespace Chunnel.Core.Enums;

/// <summary>
/// Режим TCP-соединения
/// </summary>
public enum TcpMode : byte
{
  /// <summary>
  /// TCP-клиент
  /// </summary>
  TcpClient,

  /// <summary>
  /// TCP-сервер
  /// </summary>
  TcpServer,
}
