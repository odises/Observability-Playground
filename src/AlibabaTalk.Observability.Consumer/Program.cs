using AlibabaTalk.Observability.Consumer;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Prometheus;

string workerName = (args.Length > 0) ? args[0] : System.Net.Dns.GetHostName();
workerName = "worker-" + workerName;

var builder = WebApplication.CreateBuilder(args);
builder.Host.ConfigureAppConfiguration((context, cfg) =>
{
    cfg.AddEnvironmentVariables();
});

builder.Host.ConfigureLogging(workerName);
builder.Host.ConfigureAllServices(workerName);

builder.Services.AddSingleton<MetricReporter>();

var app = builder.Build();

app.UseMetricServer();

app.RegisterStartupEvents(workerName);
var config = app.Services.GetService<IConfiguration>();

await app.RunAsync(config["Hosting:Url"]);