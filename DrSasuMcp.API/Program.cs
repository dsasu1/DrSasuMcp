using DrSasuMcp.API.API;
using DrSasuMcp.API.API.Authentication;
using DrSasuMcp.API.API.Validators;
using DrSasuMcp.Tools.API;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DrSasuMcp.API
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

            // Register API Tool dependencies
            // HTTP Client Factory
            builder.Services.AddSingleton<IHttpClientFactory, HttpClientFactory>();
            
            // Authentication Handlers
            builder.Services.AddSingleton<IAuthenticationHandler, BearerAuthHandler>();
            builder.Services.AddSingleton<IAuthenticationHandler, BasicAuthHandler>();
            builder.Services.AddSingleton<IAuthenticationHandler, ApiKeyAuthHandler>();
            builder.Services.AddSingleton<IAuthenticationHandler, CustomAuthHandler>();
            
            // Response Validators
            builder.Services.AddSingleton<IResponseValidator, StatusCodeValidator>();
            builder.Services.AddSingleton<IResponseValidator, HeaderValidator>();
            builder.Services.AddSingleton<IResponseValidator, JsonPathValidator>();
            builder.Services.AddSingleton<IResponseValidator, ResponseTimeValidator>();
            builder.Services.AddSingleton<IResponseValidator, BodyContainsValidator>();
            builder.Services.AddSingleton<IResponseValidator, BodyEqualsValidator>();
            builder.Services.AddSingleton<IResponseValidator, BodyRegexValidator>();
            
            // API Tool
            builder.Services.AddSingleton<APITool>();

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

