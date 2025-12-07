using System.IO.Ports;

namespace Chunnel.Core.Models;

/// <summary>
/// Параметры последовательного порта
/// </summary>
/// <param name="PortName">Наименование порта</param>
/// <param name="BaudRate">Скорость обмена</param>
/// <param name="Parity">Контроль четности</param>
/// <param name="DataBits">Количество информационных бит</param>
/// <param name="StopBits">Количество стоп бит</param>
public sealed record SerialPortConfig(string PortName, int BaudRate, Parity Parity, int DataBits, StopBits StopBits) : ConfigBase;