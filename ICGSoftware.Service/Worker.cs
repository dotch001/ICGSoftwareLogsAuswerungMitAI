using ICGSoftware.Library.EmailVersenden;
using ICGSoftware.Library.Logging;
using ICGSoftware.Library.LogsAuswerten;

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
            LoggingClass.LoggerFunction("Info", "Worker started");

            try
            {
                string aiResponse = await FilterErrAndAskAIClass.FilterErrAndAskAI(stoppingToken);
                await EmailVersendenClass.Process(aiResponse);

                LoggingClass.LoggerFunction("Info", "Worker finished");

                Environment.Exit(0);

            }
            catch (Exception ex)
            {
                LoggingClass.LoggerFunction("Error", ex + " Worker crashed");
                Environment.Exit(1);
            }
        }


    }
}
