using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Chunnel.Model.Config;
using CommandLine;

namespace Chunnel.Model.Args
{
  internal class Options
  {
    [Option("left_type", Required = true, HelpText = "HelpTextLeftType", ResourceType = typeof(Properties.Resources))]
    public string LeftType { get; set; }

    [Option("left_address", Default = "0.0.0.0", HelpText = "HelpTextLeftAddress", ResourceType = typeof(Properties.Resources))]
    public string LeftAddress { get; set; }

    [Option("left_port", Default = 5050, HelpText = "HelpTextLeftPort", ResourceType = typeof(Properties.Resources))]
    public int LeftPort { get; set; }

    [Option("right_type", Required = true, HelpText = "HelpTextRightType", ResourceType = typeof(Properties.Resources))]
    public string RightType { get; set; }

    [Option("right_address", Default = "0.0.0.0", HelpText = "HelpTextRightAddress", ResourceType = typeof(Properties.Resources))]
    public string RightAddress { get; set; }

    [Option("right_port", Default = 5050, HelpText = "HelpTextRightPort", ResourceType = typeof(Properties.Resources))]
    public int RightPort { get; set; }

    public ConnectionPoint GetLeft()
    {
      return Get(LeftType, LeftAddress, LeftPort);
    }

    public ConnectionPoint GetRight()
    {
      return Get(RightType, RightAddress, RightPort);
    }

    private static ConnectionPoint Get(string type, string address, int port)
    {
      switch (type.ToLower())
      {
        case "client":
          return new TcpConnectionPoint(address, port, TcpMode.Client);

        case "server":
          return new TcpConnectionPoint(address, port, TcpMode.Server);

        default:
          throw new ArgumentException(string.Format(Properties.Resources.UnsupportedEndPointType, type));
      }
    }
  }
}
