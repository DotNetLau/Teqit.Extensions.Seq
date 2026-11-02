using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using System;

namespace Teqit.Extensions.Seq
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add Serilog logging to SEQ.
        /// </summary>
        /// <param name="services">The dependency injection ServiceCollection object.</param>
        /// <param name="url">The address and port on which the SEQ instance runs.</param>
        /// <param name="level">The minimum logging level. This is the only level filter: because Serilog replaces the
        /// ILoggerFactory, the Logging:LogLevel section of appsettings does not apply to these events.</param>
        /// <param name="apiKey">The API key.</param>
        /// <param name="useDefaultDockerContainerUrl">Define true to use the host.internal.docker URL.</param>
        /// <param name="filePath">The path for the rolling buffer file.</param>
        /// <param name="flushToDiskIntervalInSeconds">Amount of seconds before flushing logs to disk.</param>
        /// <param name="rollingInterval">The interval before creating a new rolling file.</param>
        /// <param name="roleOnFileSizeLimit">The size limit in MB before a creating new rolling fire.</param>
        /// <param name="retainedFileCountLimit">The amount of rolling files to retain in total.</param>
        /// <param name="writeToProviders">Also forward events to the other registered ILoggerProviders (for example
        /// OpenTelemetry). Their built-in console provider then prints next to the Console sink; call
        /// Logging.ClearProviders() first to avoid duplicates.</param>
        /// <param name="configure">Last-word hook on the Serilog configuration, for example to add
        /// MinimumLevel.Override rules.</param>
        /// <returns>IServiceCollection</returns>
        public static IServiceCollection AddSeq(this IServiceCollection services,
            string url = "http://localhost:5341",
            LogEventLevel level = LogEventLevel.Verbose,
            string apiKey = null,
            bool useDefaultDockerContainerUrl = false,
            string filePath = "C:/Temp/LogFiles/",
            int flushToDiskIntervalInSeconds = 30,
            RollingInterval rollingInterval = RollingInterval.Day,
            bool roleOnFileSizeLimit = true,
            int retainedFileCountLimit = 31,
            bool writeToProviders = false,
            Action<LoggerConfiguration> configure = null
        )
        {
            var seqUrl = useDefaultDockerContainerUrl ? "http://host.docker.internal:5341" : url;

            services.AddSerilog((serviceProvider, logBuilder) =>
            {
                logBuilder
                    .MinimumLevel.Is(level)
                    .ReadFrom.Services(serviceProvider)
                    .Enrich.FromLogContext()
                    .WriteTo.Seq(seqUrl, level, apiKey: apiKey)
                    .WriteTo.Console(restrictedToMinimumLevel: level)
                    .WriteTo.File(
                        path: filePath,
                        restrictedToMinimumLevel: level,
                        flushToDiskInterval: TimeSpan.FromSeconds(flushToDiskIntervalInSeconds),
                        rollingInterval: rollingInterval,
                        rollOnFileSizeLimit: roleOnFileSizeLimit,
                        retainedFileCountLimit: retainedFileCountLimit
                    )
                    .Enrich.WithEnvironmentName()
                    .Enrich.WithEnvironmentUserName()
                    .Enrich.WithMachineName()
                    .Enrich.WithProcessId()
                    .Enrich.WithProcessName()
                    .Enrich.WithThreadId()
                    .Enrich.WithThreadName();

                var hostEnvironment = serviceProvider.GetService<IHostEnvironment>();
                if (hostEnvironment != null)
                {
                    logBuilder.Enrich.WithProperty("Application", hostEnvironment.ApplicationName);
                }

                configure?.Invoke(logBuilder);
            }, writeToProviders: writeToProviders);

            return services;
        }
    }
}
