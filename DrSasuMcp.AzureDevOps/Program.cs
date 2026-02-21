using DrSasuMcp.AzureDevOps.AzureDevOps;
using DrSasuMcp.AzureDevOps.AzureDevOps.Analyzers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DrSasuMcp.AzureDevOps
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

            // Register HttpClient via factory (avoids socket exhaustion from new HttpClient())
            builder.Services.AddHttpClient(nameof(AzureDevOpsService));

            // Register Azure DevOps Tool dependencies
            builder.Services.AddSingleton<IAzureDevOpsService, AzureDevOpsService>();
            builder.Services.AddSingleton<IDiffService, DiffService>();
            
            // Code Analyzers
            builder.Services.AddSingleton<ICodeAnalyzer, SecurityAnalyzer>();
            builder.Services.AddSingleton<ICodeAnalyzer, CodeQualityAnalyzer>();
            builder.Services.AddSingleton<ICodeAnalyzer, BestPracticesAnalyzer>();
            
            // Azure DevOps Tool
            builder.Services.AddSingleton<AzureDevOpsTool>();

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

