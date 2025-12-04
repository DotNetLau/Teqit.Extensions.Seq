using Microsoft.Extensions.DependencyInjection;
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
        /// <param name="level">The minimum logging level.</param>
        /// <param name="apiKey">The API key.</param>
        /// <param name="useDefaultDockerContainerUrl">Define true to use the host.internal.docker URL.</param>
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
            int retainedFileCountLimit = 31
        )
        {
            string seqUrl = useDefaultDockerContainerUrl ? "http://host.docker.internal:5341" : url;

            services.AddSerilog((logBuilder) =>
            {
                logBuilder
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
            });

            return services;
        }
    }
}