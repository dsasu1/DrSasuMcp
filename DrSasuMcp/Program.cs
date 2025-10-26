﻿using DrSasuMcp.Tools.SQL;
using DrSasuMcp.Tools.API;
using DrSasuMcp.Tools.API.Authentication;
using DrSasuMcp.Tools.API.Validators;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DrSasuMcp
{
    public  class Program
    {
        public static async Task Main(string[] args)
        {
            // Create the application host builder
            var builder = Host.CreateApplicationBuilder(args);

            // Configure console logging with Trace level
            _ = builder.Logging.AddConsole(consoleLogOptions =>
            {
                consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
            });

            // Register SQL Tool dependencies
            _ = builder.Services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();
            _ = builder.Services.AddSingleton<SQLTool>();

            // Register API Tool dependencies
            // HTTP Client Factory
            _ = builder.Services.AddSingleton<DrSasuMcp.Tools.API.IHttpClientFactory, HttpClientFactory>();
            
            // Authentication Handlers
            _ = builder.Services.AddSingleton<IAuthenticationHandler, BearerAuthHandler>();
            _ = builder.Services.AddSingleton<IAuthenticationHandler, BasicAuthHandler>();
            _ = builder.Services.AddSingleton<IAuthenticationHandler, ApiKeyAuthHandler>();
            _ = builder.Services.AddSingleton<IAuthenticationHandler, CustomAuthHandler>();
            
            // Response Validators
            _ = builder.Services.AddSingleton<IResponseValidator, StatusCodeValidator>();
            _ = builder.Services.AddSingleton<IResponseValidator, HeaderValidator>();
            _ = builder.Services.AddSingleton<IResponseValidator, JsonPathValidator>();
            _ = builder.Services.AddSingleton<IResponseValidator, ResponseTimeValidator>();
            _ = builder.Services.AddSingleton<IResponseValidator, BodyContainsValidator>();
            _ = builder.Services.AddSingleton<IResponseValidator, BodyEqualsValidator>();
            _ = builder.Services.AddSingleton<IResponseValidator, BodyRegexValidator>();
            
            // API Tool
            _ = builder.Services.AddSingleton<APITool>();

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
