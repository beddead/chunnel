using System.CommandLine;
using System.CommandLine.Parsing;
using System.IO.Ports;
using System.Text.RegularExpressions;
using Chunnel.Core.Enums;
using Chunnel.Core.Models;

namespace Chunnel;

internal sealed partial class AppCommand
{
  public AppCommand()
  {
    _rootCmd = new RootCommand("Chunnel app");

    _leftTypeOption = new Option<string>(Options.LeftType)
    {
      Description = "Тип точки соединения слева",
      Required = true
    };
    _leftTypeOption.AcceptOnlyFromAmong(ConnectionTypes.All);
    _rootCmd.Options.Add(_leftTypeOption);

    _leftConfigOption = new Option<string>(Options.LeftParams)
    {
      Description = """
      Параметры точки соединения слева.
      Формат строки зависит от типа соединения:
        - serial-port: <port.name>:<baud.rate>:<8N1|8E1|8O1|7N1|7E1|7O1>, например COM1:9600:8N1, или /dev/tty0:9600:8N1
        - tcp-client: ip_address:tcp_port, например 192.168.91.2:4001
        - tcp-server: network_address:tcp_port, например 0.0.0.0:1983
      """,
      Required = true
    };
    _leftConfigOption.Validators.Add(ValidateParams);
    _rootCmd.Options.Add(_leftConfigOption);

    _rightTypeOption = new Option<string>(Options.RightType)
    {
      Description = "Тип точки соединения справа",
      Required = true
    };
    _rightTypeOption.AcceptOnlyFromAmong(ConnectionTypes.All);
    _rootCmd.Options.Add(_rightTypeOption);

    _rightConfigOption = new Option<string>(Options.RightParams)
    {
      Description = """
      Параметры точки соединения справа.
      Формат строки зависит от типа соединения:
        - serial-port: <port.name>:<baud.rate>:<8N1|8E1|8O1|7N1|7E1|7O1>, например COM1:9600:8N1, или /dev/tty0:9600:8N1
        - tcp-client: ip_address:tcp_port, например 192.168.91.2:4002
        - tcp-server: network_address:tcp_port, например 127.0.0.1:2000
      """,
      Required = true,
    };
    _rightConfigOption.Validators.Add(ValidateParams);
    _rootCmd.Options.Add(_rightConfigOption);
  }

  public int Execute(string[] args, Func<ConfigBase, ConfigBase, int> task)
  {
    _rootCmd.SetAction(parseResult =>
    {
      if (parseResult.Errors.Count != 0)
        return ExitCodes.WrongParams;

      var leftTypeString = parseResult.GetValue(_leftTypeOption);
      var leftConfigString = parseResult.GetValue(_leftConfigOption);
      var rightTypeString = parseResult.GetValue(_rightTypeOption);
      var rightConfigString = parseResult.GetValue(_rightConfigOption);
      if (leftTypeString is null || leftConfigString is null || rightTypeString is null || rightConfigString is null)
        return ExitCodes.WrongParams;

      var leftConfig = BuildConfig(leftTypeString, leftConfigString);
      var rightConfig = BuildConfig(rightTypeString, rightConfigString);
      if (leftConfig is null || rightConfig is null)
        return ExitCodes.WrongParams;

      return task(leftConfig, rightConfig);
    });

    return _rootCmd.Parse(args).Invoke();
  }

  public static class Options
  {
    public const string LeftType = "left-type";
    public const string LeftParams = "left-params";
    public const string RightType = "right-type";
    public const string RightParams = "right-params";
  }

  /// <summary>
  /// Типы соединений
  /// </summary>
  public static class ConnectionTypes
  {
    public const string SerialPort = "serial-port";
    public const string TcpClient = "tcp-client";
    public const string TcpServer = "tcp-server";
    public readonly static string[] All = [SerialPort, TcpClient, TcpServer];
  }

  public static class ExitCodes
  {
    public const int WrongParams = 1;
  }

  private static ConfigBase? BuildConfig(string type, string @params)
  {
    return type switch
    {
      ConnectionTypes.SerialPort => BuildSerialPortConfig(@params),
      ConnectionTypes.TcpClient or ConnectionTypes.TcpServer => BuildTcpConfig(type, @params),
      _ => null
    };
  }

  private static TcpConfig? BuildTcpConfig(string type, string @params)
  {
    var regEx = TcpParamsRegEx();
    var match = regEx.Match(@params);
    if (match.Success is false)
      return null;

    if (int.TryParse(match.Groups[2].ValueSpan, out var tcpPort) is false)
      return null;

    if (tcpPort is 0 or > 65535)
      return null;

    var tcpMode = type is ConnectionTypes.TcpClient ? TcpMode.TcpClient : TcpMode.TcpServer;

    return new TcpConfig(tcpMode, match.Groups[1].Value, tcpPort);
  }

  private static SerialPortConfig? BuildSerialPortConfig(string @params)
  {
    var regEx = SerialPortParamsRegEx();
    var match = regEx.Match(@params);
    if (match.Success is false)
      return null;

    var portName = match.Groups[1].Value;

    if (int.TryParse(match.Groups[2].ValueSpan, out var baudRate) is false)
      return null;
    if (baudRate is < 300 or > 115200)
      return null;

    if (int.TryParse(match.Groups[3].ValueSpan, out var bitsCount) is false)
      return null;
    if (bitsCount is < 7 or > 8)
      return null;

    var parity = match.Groups[4].Value[0] switch
    {
      'N' or 'n' => Parity.None,
      'O' or 'o' => Parity.Odd,
      'E' or 'e' => Parity.Even,
      _ => Parity.None
    };

    if (int.TryParse(match.Groups[5].ValueSpan, out var stopBitsValue) is false)
      return null;
    if (stopBitsValue is < 1 or > 2)
      return null;
    var stopBits = stopBitsValue switch
    {
      1 => StopBits.One,
      2 => StopBits.Two,
      _ => StopBits.One
    };

    return new(portName, baudRate, parity, bitsCount, stopBits);
  }

  private void ValidateParams(OptionResult result)
  {
    if (result.IdentifierToken is null)
      return;

    switch (result.IdentifierToken.Value)
    {
      case Options.LeftParams:
        ValidateParamsForType(result, result.GetRequiredValue(_leftTypeOption), result.GetRequiredValue(_leftConfigOption));
        break;

      case Options.RightParams:
        ValidateParamsForType(result, result.GetRequiredValue(_rightTypeOption), result.GetRequiredValue(_rightConfigOption));
        break;
    }
  }

  private static void ValidateParamsForType(OptionResult result, string type, string @params)
  {
    switch (type)
    {
      case ConnectionTypes.SerialPort:
        ValidateSerialPortParams(result, @params);
        break;

      case ConnectionTypes.TcpClient:
      case ConnectionTypes.TcpServer:
        ValidateTcpParams(result, @params);
        break;

      default:
        break;
    };
  }

  private static void ValidateTcpParams(OptionResult result, string @params)
  {
    var checkRegEx = TcpParamsRegEx();
    if (checkRegEx.IsMatch(@params) is false)
      result.AddError($"Неверный формат параметров для TCP/IP: {result.IdentifierToken!.Value} = {@params}");
  }

  private static void ValidateSerialPortParams(OptionResult result, string @params)
  {
    var checkRegEx = SerialPortParamsRegEx();
    if (checkRegEx.IsMatch(@params) is false)
      result.AddError($"Неверный формат параметров для последовательного порта: {result.IdentifierToken!.Value} = {@params}");
  }

  [GeneratedRegex(@"([\w\/\d]+):(\d+):([78])([NnEeOo])([12])")]
  private static partial Regex SerialPortParamsRegEx();

  [GeneratedRegex(@"(\b25[0-5]|\b2[0-4][0-9]|\b[01]?[0-9][0-9]?)(\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)){3}:(\d+)")]
  private static partial Regex TcpParamsRegEx();

  private readonly RootCommand _rootCmd;
  private readonly Option<string> _leftTypeOption;
  private readonly Option<string> _leftConfigOption;
  private readonly Option<string> _rightTypeOption;
  private readonly Option<string> _rightConfigOption;
}
