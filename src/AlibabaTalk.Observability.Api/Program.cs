using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Diagnostics;
using Prometheus;
using AlibabaTalk.Observability.Web;
using Microsoft.Extensions.DependencyInjection;

var appBuilder = WebApplication.CreateBuilder(args);
appBuilder.Host.ConfigureAppConfiguration((context, cfg) =>
{
    cfg.AddEnvironmentVariables();
});

appBuilder.Host.ConfigureLogging();
appBuilder.Host.ConfigureAllServices();

appBuilder.Services.AddSingleton<MetricReporter>();


var app = appBuilder.Build();

app.UseMetricServer();
app.UseMiddleware<ResponseMetricMiddleware>();

app.UseLoadTestMiddleware();

app.UseTraceIdentifierOnResponse();

app.UseExceptionHandler(new ExceptionHandlerOptions()
{
    ExceptionHandler = async (e) =>
    {
        var exceptionObject = e.Features.Get<IExceptionHandlerFeature>();

        if (exceptionObject.Error is OperationCanceledException)
        {
            e.Response.StatusCode = 408;
            e.Response.Headers.Add("traceId", System.Diagnostics.Activity.Current.Id);

            await e.Response.BodyWriter.WriteAsync(System.Text.Encoding.UTF8.GetBytes("timeout"));
            await e.Response.CompleteAsync();
        }
    }
});

app.UseRouting();
app.UseEndpoints((c) =>
{
    c.MapControllers();
});

app.RegisterStartupEvents();
await app.RunAsync();
