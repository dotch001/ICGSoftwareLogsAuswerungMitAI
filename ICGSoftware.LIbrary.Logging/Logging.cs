using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
using Microsoft.Extensions.Configuration.Json;
using System.ComponentModel;
using Serilog.Core;
using Microsoft.Extensions.Logging;

namespace ICGSoftware.Library.Logging
{
    public class LoggingClass
    {
        public static void LoggerFunction(string TypeOfMessage, string message)
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("applicationSettings_Logging.json")
                .Build();

            var settings = config.GetSection("ApplicationSettings").Get<ApplicationSettingsClass>();

            if (!Directory.Exists(settings.outputFolder)) { Directory.CreateDirectory(settings.outputFolder); }

            int i = 0;

            string outputFile = settings.outputFolder + "\\" + settings.logFileName + i + ".log";

            bool isLoggerConfigured = Log.Logger != Logger.None;

            while (File.Exists(outputFile) && new FileInfo(outputFile).Length /1024 >= 300)
            {
                i++;
                outputFile = settings.outputFolder + "\\" + settings.logFileName + i + ".log";
            }
            if (!isLoggerConfigured)
            {
                Log.Logger = new LoggerConfiguration().WriteTo.File(outputFile).CreateLogger();
            }


            if(TypeOfMessage == "Info") { 
                Log.Information(message);
            }
            else if(TypeOfMessage == "Warning") {
                Log.Warning(message);
            }
            else if(TypeOfMessage == "Error") {
                Log.Error(message);
            }
            else if(TypeOfMessage == "Debug") {
                Log.Debug(message);
            }
        }


            
        
    }
}
