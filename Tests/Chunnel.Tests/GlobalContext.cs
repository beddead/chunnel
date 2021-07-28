using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace Chunnel.Tests
{
  [SetUpFixture]
  public class GlobalContext
  {
    [OneTimeSetUp]
    public void GlobalSetup()
    {
      LoggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
      {
        builder.SetMinimumLevel(LogLevel.Trace);
        builder.AddDebug();
      });
    }

    public static ILoggerFactory LoggerFactory { get; private set; } 
  }
}
