using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
using Microsoft.Extensions.Configuration.Json;
using System.ComponentModel;

namespace ICGSoftware.Library.Logging
{
    public class LoggingClass
    {
        public static void LogInformation(string message)
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("applicationSettings_Logging.json")
                .Build();

            var settings = config.GetSection("ApplicationSettings").Get<ApplicationSettingsClass>();

            if (!Directory.Exists(settings.outputFolder)) { Directory.CreateDirectory(settings.outputFolder); }

            Log.Logger = new LoggerConfiguration().WriteTo.File(settings.outputFolder + "\\" + settings.logFileName + ".log", rollingInterval: RollingInterval.Day).CreateLogger();
            Log.Information(message);
        }
    }
}
