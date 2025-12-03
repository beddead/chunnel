using System.CommandLine;

namespace Chunnel;

internal sealed class Command
{
  public Command()
  {
    _rootCmd = new RootCommand("Chunnel app");

    var leftTypeOption = new Option<string>("left-type")
    {
      Description = "Тип точки соединения слева",
      Required = true
    };
    leftTypeOption.AcceptOnlyFromAmong(ConnectionTypes.All);
    _rootCmd.Options.Add(leftTypeOption);

    var rightTypeOption = new Option<string>("right-type")
    {
      Description = "Тип точки соединения справа",
      Required = true
    };
    rightTypeOption.AcceptOnlyFromAmong(ConnectionTypes.All);
    _rootCmd.Options.Add(rightTypeOption);
  }

  public int Execute(string[] args)
  {
    return _rootCmd.Parse(args).Invoke();
  }

  /// <summary>
  /// Типы соединений
  /// </summary>
  private static class ConnectionTypes
  {
    public const string SerialPort = "serial-port";
    public const string TcpClient = "tcp-client";
    public const string TcpServer = "tcp-server";
    public readonly static string[] All = [SerialPort, TcpClient, TcpServer];
  }

  private readonly RootCommand _rootCmd;
}
