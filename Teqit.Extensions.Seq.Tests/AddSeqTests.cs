using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using Serilog.Context;
using Serilog.Core;
using Serilog.Events;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Teqit.Extensions.Seq.Tests;

public class AddSeqTests
{
    private const string Unreachable = "http://127.0.0.1:1";

    private sealed class CapturingSink : ILogEventSink
    {
        public List<LogEvent> Events { get; } = new();
        public void Emit(LogEvent logEvent) { lock (Events) Events.Add(logEvent); }
    }

    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Test";
        public string ApplicationName { get; set; } = "TestApp";
        public string ContentRootPath { get; set; } = ".";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class CapturingProvider : ILoggerProvider, ILogger
    {
        public List<string> Messages { get; } = new();
        public ILogger CreateLogger(string categoryName) => this;
        public void Dispose() { }
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            => Messages.Add(formatter(state, exception));
    }

    private static void Run(Action<IServiceCollection, string> register, Action<ILoggerFactory> log,
        bool hostEnvironment = true, CapturingSink sink = null)
    {
        var dir = Path.Combine(Path.GetTempPath(), "seq-tests-" + Guid.NewGuid());
        Directory.CreateDirectory(dir);
        try
        {
            var services = new ServiceCollection();
            if (hostEnvironment) services.AddSingleton<IHostEnvironment>(new FakeHostEnvironment());
            if (sink != null) services.AddSingleton<ILogEventSink>(sink);
            register(services, Path.Combine(dir, "log-"));
            using (var sp = services.BuildServiceProvider())
            {
                log(sp.GetRequiredService<ILoggerFactory>());
            }
        }
        finally
        {
            Log.CloseAndFlush();
            try { Directory.Delete(dir, true); } catch { }
        }
    }

    [Fact]
    public void Debug_level_captures_debug_and_information()
    {
        var sink = new CapturingSink();
        Run((s, f) => s.AddSeq(url: Unreachable, level: LogEventLevel.Debug, filePath: f),
            lf => { var l = lf.CreateLogger("A"); l.LogDebug("d"); l.LogInformation("i"); }, sink: sink);
        Assert.Equal(2, sink.Events.Count);
    }

    [Fact]
    public void Information_level_drops_debug()
    {
        var sink = new CapturingSink();
        Run((s, f) => s.AddSeq(url: Unreachable, level: LogEventLevel.Information, filePath: f),
            lf => lf.CreateLogger("A").LogDebug("d"), sink: sink);
        Assert.Empty(sink.Events);
    }

    [Fact]
    public void Default_level_captures_trace()
    {
        var sink = new CapturingSink();
        Run((s, f) => s.AddSeq(url: Unreachable, filePath: f),
            lf => lf.CreateLogger("A").LogTrace("t"), sink: sink);
        Assert.Single(sink.Events);
    }

    [Fact]
    public void Events_have_Application_property()
    {
        var sink = new CapturingSink();
        Run((s, f) => s.AddSeq(url: Unreachable, filePath: f),
            lf => lf.CreateLogger("A").LogInformation("i"), sink: sink);
        var e = Assert.Single(sink.Events);
        Assert.Equal("\"TestApp\"", e.Properties["Application"].ToString());
    }

    [Fact]
    public void Without_host_environment_no_Application_and_no_exception()
    {
        var sink = new CapturingSink();
        Run((s, f) => s.AddSeq(url: Unreachable, filePath: f),
            lf => lf.CreateLogger("A").LogInformation("i"), hostEnvironment: false, sink: sink);
        var e = Assert.Single(sink.Events);
        Assert.False(e.Properties.ContainsKey("Application"));
    }

    [Fact]
    public void LogContext_properties_are_captured()
    {
        var sink = new CapturingSink();
        Run((s, f) => s.AddSeq(url: Unreachable, filePath: f),
            lf =>
            {
                using (LogContext.PushProperty("X", 1))
                    lf.CreateLogger("A").LogInformation("i");
            }, sink: sink);
        var e = Assert.Single(sink.Events);
        Assert.True(e.Properties.ContainsKey("X"));
    }

    [Fact]
    public void Configure_hook_applies_minimum_level_override()
    {
        var sink = new CapturingSink();
        Run((s, f) => s.AddSeq(url: Unreachable, filePath: f,
                configure: cfg => cfg.MinimumLevel.Override("Noisy", LogEventLevel.Warning)),
            lf =>
            {
                lf.CreateLogger("Noisy.Thing").LogInformation("noisy");
                lf.CreateLogger("Other").LogInformation("other");
            }, sink: sink);
        var e = Assert.Single(sink.Events);
        Assert.Equal("Other", e.Properties["SourceContext"].ToString().Trim('"'));
    }

    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 0)]
    public void WriteToProviders_controls_forwarding(bool writeToProviders, int expected)
    {
        var provider = new CapturingProvider();
        Run((s, f) =>
            {
                s.AddSingleton<ILoggerProvider>(provider);
                s.AddSeq(url: Unreachable, filePath: f, writeToProviders: writeToProviders);
            },
            lf => lf.CreateLogger("A").LogInformation("hello"));
        Assert.Equal(expected, provider.Messages.Count);
    }
}
