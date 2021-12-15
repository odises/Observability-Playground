using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Diagnostics;
using Prometheus;
using AlibabaTalk.Observability.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
app.UseTraceIdentifierOnResponse();

app.UseLoadTestMiddleware();


app.UseExceptionHandler(new ExceptionHandlerOptions()
{
    ExceptionHandler = async (e) =>
    {
        var exceptionObject = e.Features.Get<IExceptionHandlerFeature>();


        if (exceptionObject.Error is OperationCanceledException)
        {
            e.Response.StatusCode = 408;

            var logger = e.RequestServices.GetService<ILogger<WebApplication>>();

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
