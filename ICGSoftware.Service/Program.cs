using ICGSoftware.Service;
using Microsoft.Extensions.Hosting.WindowsServices;

class Program
{
    public static async Task Main(string[] args)
    {
        using var cts = new CancellationTokenSource();

        Console.CancelKeyPress += (sender, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cts.Cancel();
        };

        await Host.CreateDefaultBuilder(args)
        .UseWindowsService() // Ensure the Microsoft.Extensions.Hosting.WindowsServices package is installed  
        .ConfigureServices(services =>
        {
            services.AddHostedService<Worker>();
        })
        .ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
        })
        .Build()
        .RunAsync(cts.Token);
    }
}