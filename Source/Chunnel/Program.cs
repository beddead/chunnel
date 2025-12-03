using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Chunnel;

internal sealed class Program
{
  private static int Main(string[] args)
  {
    SetUpCulture();
    using var services = BuildServices();

    /*
    var logger = services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("\u001b[91mHello\u001b[0m");
    logger.LogWarning("\u001b[32mWorld\u001b[0m");
    logger.LogWarning("\u001b[92mTime: {Time}\u001b[0m", DateTime.Now);
    */

    var command = new Command();
    return command.Execute(args);
  }

  private static ServiceProvider BuildServices()
  {
    return new ServiceCollection()
      .AddLogging(o => o.ClearProviders().AddSimpleConsole(o =>
      {
        var ci = CultureInfo.CurrentUICulture;
        o.IncludeScopes = false;
        o.SingleLine = true;
        o.TimestampFormat = $"{ci.DateTimeFormat.ShortDatePattern} {ci.DateTimeFormat.LongTimePattern}.fff ";
      }))
      .BuildServiceProvider();
  }

  private static void SetUpCulture()
  {
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
      return;

    if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && !RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
      return;

    var lang = Environment.GetEnvironmentVariable("LANG");
    if (string.IsNullOrEmpty(lang))
      return;

    var cultureName = lang.Split('.')[0].Replace('_', '-');
    CultureInfo.DefaultThreadCurrentCulture = new CultureInfo(cultureName);
    CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.DefaultThreadCurrentCulture;
    CultureInfo.CurrentCulture = CultureInfo.DefaultThreadCurrentCulture;
    CultureInfo.CurrentUICulture = CultureInfo.CurrentCulture;
  }
}