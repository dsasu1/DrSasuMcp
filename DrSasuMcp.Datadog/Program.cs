using DrSasuMcp.Datadog.Datadog;
using DrSasuMcp.Datadog.Datadog.Troubleshooters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DrSasuMcp.Datadog
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // Create the application host builder
            var builder = Host.CreateApplicationBuilder(args);

            // Configure console logging with Trace level
            builder.Logging.AddConsole(consoleLogOptions =>
            {
                consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
            });

            // Register Datadog Tool dependencies
            builder.Services.AddSingleton<IDatadogService, DatadogService>();

            // Datadog Troubleshooters
            builder.Services.AddSingleton<ITroubleshooter, MetricsTroubleshooter>();
            builder.Services.AddSingleton<ITroubleshooter, LogsTroubleshooter>();
            builder.Services.AddSingleton<ITroubleshooter, TracesTroubleshooter>();
            builder.Services.AddSingleton<ITroubleshooter, ErrorTrackingTroubleshooter>();
            builder.Services.AddSingleton<ITroubleshooter, ServiceMapTroubleshooter>();

            // Datadog Tool
            builder.Services.AddSingleton<DatadogTool>();

            // Configure MCP server
            builder.Services
                .AddMcpServer()
                .WithStdioServerTransport()
                .WithToolsFromAssembly();

            var host = builder.Build();

            // Setup cancellation token for graceful shutdown (Ctrl+C or SIGTERM)
            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true; // Prevent the process from terminating immediately
                cts.Cancel();
            };

            try
            {
                // Run the host with cancellation support
                await host.RunAsync(cts.Token);
            }
            catch (Exception ex)
            {
                // Attempt to log the exception using the host's logger
                if (host.Services.GetService(typeof(ILogger<Program>)) is ILogger<Program> logger)
                {
                    logger.LogCritical(ex, "Unhandled exception occurred during host execution.");
                }
                else
                {
                    Console.Error.WriteLine($"Unhandled exception: {ex}");
                }

                // Set a non-zero exit code
                Environment.ExitCode = 1;
            }
        }
    }
}

