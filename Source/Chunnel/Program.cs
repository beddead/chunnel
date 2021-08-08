using Chunnel.Model;
using Chunnel.Model.Args;
using CommandLine;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Chunnel
{
  internal class Program
  {
    private static async Task Main(string[] args)
    {
      Setup();

      var logger = _loggerFactory.CreateLogger<Program>();

      var result = Parser.Default.ParseArguments<Options>(args);

      try
      {
        await result.WithParsedAsync(options => RunAsync(options));
      }
      catch (Exception)
      {
        _loggerFactory.Dispose();
      }
    }

    private static Task RunAsync(Options options)
    {
      var runner = new Runner(options, _loggerFactory);
      _cancellation = new CancellationTokenSource();

      return runner.RunAsync(_cancellation.Token);
    }

    private static void Setup()
    {
      _loggerFactory = LoggerFactory.Create(builder =>
      {
        builder.AddSimpleConsole(options =>
        {
          options.IncludeScopes = true;
          options.SingleLine = true;
          options.TimestampFormat = "hh:mm:ss ";
        });
      });

    }

    private static ILoggerFactory _loggerFactory;
    private static CancellationTokenSource _cancellation;
  }
}
