namespace Chunnel.Core.Models;

/// <summary>
/// Параметры TCP-соединения
/// </summary>
/// <param name="Address">IP-адрес</param>
/// <param name="Port">TCP-порт</param>
internal sealed record TcpConfig(string Address, int Port);
