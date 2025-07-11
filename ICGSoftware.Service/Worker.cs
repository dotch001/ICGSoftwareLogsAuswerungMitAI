using ICGSoftware.Library;
using ICGSoftware.Library.EmailVersenden;
using ICGSoftware.Library.ErrorsKategorisierenUndZählen;
using ICGSoftware.Library.Logging;
using ICGSoftware.Library.LogsAuswerten;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

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
            string AiResponse = await FilterErrAndAskAIClass.FilterErrAndAskAI();

            await EmailVersendenClass.Process(AiResponse);

            


            while (!stoppingToken.IsCancellationRequested)
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                }
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}

