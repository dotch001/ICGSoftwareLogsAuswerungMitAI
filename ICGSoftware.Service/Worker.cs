using ICGSoftware.Library.EmailVersenden;
using ICGSoftware.Library.Logging;
using ICGSoftware.Library.LogsAuswerten;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ICGSoftware.Service
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            LoggingClass.LogInformation("Worker started at: " + DateTimeOffset.Now);

            try
            {
                string aiResponse = await FilterErrAndAskAIClass.FilterErrAndAskAI(stoppingToken);
                await EmailVersendenClass.Process(aiResponse);

                LoggingClass.LogInformation("Worker finished at: " +  DateTimeOffset.Now);
                Environment.Exit(0);

            }
            catch (Exception ex)
            {
                LoggingClass.LogInformation(ex + " Worker crashed");
            }
        }


    }
}
