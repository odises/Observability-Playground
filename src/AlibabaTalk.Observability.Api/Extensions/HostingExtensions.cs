using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTracing;
using System.Linq;
using Microsoft.Extensions.Hosting;

public static class HostingExtensions
{
    public static void ConfigureLogging(this ConfigureHostBuilder host)
    {
        host.UseSerilog((ctx, cfg) =>
       {
           cfg.ReadFrom.Configuration(ctx.Configuration)
               .Enrich.FromLogContext()
               .Filter.ByExcluding(c => c.Properties.Any(p => p.Value.ToString().Contains("metrics")))
               .Enrich.WithProperty("service_name", "gateway")
               .Enrich.WithTraceIdentifier();
       });
    }

    public static IHostBuilder ConfigureAllServices(this ConfigureHostBuilder host)
    {
        return host.ConfigureServices((context, services) =>
         {
             services.AddHttpContextAccessor();
             services.AddControllers();
             services.AddHttpClient();

             services.AddScoped<SearchService>();
             services.AddSingleton<Bus>();

             services.AddOpenTelemetryTracing((sp, cfg) =>
             {
                 var configuration = sp.GetRequiredService<IConfiguration>();
                 var resourceBuilder = OpenTelemetry.Resources.ResourceBuilder.CreateDefault();
                 resourceBuilder.AddService("opentracing.Gateway");

                 cfg.SetResourceBuilder(resourceBuilder)
                     .AddAspNetCoreInstrumentation(options =>
                     {
                         options.Filter = (c) =>
                         {
                             return c.Request.Path.ToString().Contains("search");
                         };
                     })
                      .AddSource(nameof(ObservableConsumer))
                     .AddZipkinExporter(z =>
                     {
                         string address = configuration["Zipkin:Address"]?.ToString();
                         z.Endpoint = new Uri(address);
                     })
                     .AddConsoleExporter();
             });
         });
    }
}
