using Chunnel.Core.Enums;

namespace Chunnel.Core.Models;

/// <summary>
/// Параметры TCP-соединения
/// </summary>
/// <param name="Address">IP-адрес</param>
/// <param name="Port">TCP-порт</param>
public sealed record TcpConfig(TcpMode Type, string Address, int Port) : ConfigBase;
