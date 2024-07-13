using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;

namespace Teqit.Extensions.Seq;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add Serilog logging to SEQ.
    /// </summary>
    /// <param name="services">The dependency injection ServiceCollection object.</param>
    /// <param name="url">The address and port on which the SEQ instance runs.</param>
    /// <param name="level">The minimum logging level.</param>
    /// <param name="apiKey">The API key.</param>
    /// <returns>IServiceCollection</returns>
    public static IServiceCollection AddSeq(this IServiceCollection services, string url = "http://localhost:5341", LogEventLevel level = LogEventLevel.Verbose, string? apiKey = null)
    {
        services.AddSerilog((logBuilder) =>
        {
            logBuilder.WriteTo.Seq(url, level, apiKey: apiKey)
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