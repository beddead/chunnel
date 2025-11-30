namespace Chunnel.Core.Enums;

/// <summary>
/// Тип точки соединения
/// </summary>
public enum EndpointType : byte
{
  /// <summary>
  /// Последовательный порт
  /// </summary>
  SerialPort,

  /// <summary>
  /// TCP-клиент
  /// </summary>
  TcpClient,

  /// <summary>
  /// TCP-сервер
  /// </summary>
  TcpServer,
}
