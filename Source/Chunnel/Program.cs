using Microsoft.Extensions.Logging;
using System;

namespace Chunnel
{
  internal class Program
  {
    private static void Main(string[] args)
    {
      Setup();
      var logger = _loggerFactory.CreateLogger<Program>();
      logger.LogInformation("Hello World!");
      using (logger.BeginScope("[msg from scope]"))
        logger.LogInformation("Each log message is fit in a single line.");
      Console.ReadLine();
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
  }
}
