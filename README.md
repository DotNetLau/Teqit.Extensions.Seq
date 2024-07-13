# Microsoft Dependency Injection extensions for SEQ

Use this package during your start-up to write logging to SEQ.

It comes with a lot of enrichments by default:

- environment name,
- environment username,
- machine name,
- process Id,
- process name,
- thread Id,
- thread name.

By running the bare minimum of this package the log events will be sent as `verbose` to `http://localhost:5341` without api key.

## Changelog

1.0.2 Add apikey support and add summary for extension method

1.0.1 Add readme

1.0.0 Initial setup

## Example

The default extension method acccepts nullable parameters, and thus can be used without any parameters.

```c#
using Teqit.Extensions.Seq;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSeq();
```

If you have SEQ running on a specific URL and port, then specify it like so:

```c#
using Teqit.Extensions.Seq;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSeq("https://company.io:5341/");
```

If you want to log only errors, then specify it like so:

```c#
using Serilog;
using Teqit.Extensions.Seq;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSeq(level: Serilog.Events.LogEventLevel.Error);
```

If you want to use an API key, then specify it like so:

```c#
using Teqit.Extensions.Seq;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSeq(apiKey: "01234567890123456789");
```

Add the following to your built `WebApplication` object if you want request logging to appear in SEQ (this requires the Serilog.AspNetCore package to be installed):

```c#
using Serilog;
using Teqit.Extensions.Seq;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSeq();

var app = builder.Build();
app.UseSerilogRequestLogging();
```
