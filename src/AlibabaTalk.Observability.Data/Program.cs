using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Formatting.Elasticsearch;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddGrpc();
builder.Host.UseSerilog((ctx, cfg) =>
{
    cfg.ReadFrom.Configuration(ctx.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("service_name", "data-provider");
});

builder.Services.AddOpenTelemetryTracing((sp, cfg) =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var resourceBuilder = OpenTelemetry.Resources.ResourceBuilder.CreateDefault();
    resourceBuilder.AddService("opentracing.DataInvariant");

    cfg.SetResourceBuilder(resourceBuilder);
    cfg
        .AddSource(nameof(SearcherService))
        .AddAspNetCoreInstrumentation()
        .AddZipkinExporter(z =>
        {
            string zipkinAddress = configuration["Zipkin:Address"]?.ToString() ?? "http://localhost:9411/";
            z.Endpoint = new Uri(zipkinAddress);
        });
});

var app = builder.Build();
var config = app.Services.GetRequiredService<IConfiguration>();

app.MapGrpcService<SearcherService>();
await app.RunAsync(config["Hosting:Address"]);