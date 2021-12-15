using System;
using System.Net.Http;
using AlibabaTalk.Observability.Consumer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

public static class HostingExtensions
{
    public static void ConfigureLogging(this ConfigureHostBuilder host, string workerName)
    {
        host.UseSerilog((ctx, cfg) =>
       {
           cfg.ReadFrom.Configuration(ctx.Configuration)
              .Enrich.FromLogContext()
              .Enrich.WithProperty("service_name", workerName)
              .Enrich.WithTraceIdentifier();
       });
    }

    public static void ConfigureAllServices(this ConfigureHostBuilder host, string workerName)
    {
        host.ConfigureServices((context, services) =>
        {
            string agentName = "search.worker" + workerName;

            services
                .AddHttpClient()
                .Configure<HttpClientFactoryOptions>(options =>
                {
                    options.HttpMessageHandlerBuilderActions.Add(builder =>
                            builder.PrimaryHandler = new HttpClientHandler
                            {
                                ServerCertificateCustomValidationCallback = (m, crt, chn, e) => true
                            });
                });

            services.AddGrpcClient<Searcher.SearcherClient>((sp, cfg) =>
                {
                    var config = sp.GetRequiredService<IConfiguration>();
                    cfg.Address = new Uri(config["GrpcEndpoint:Address"]);
                }).ConfigurePrimaryHttpMessageHandler(() =>
                {
                    var httpHandler = new HttpClientHandler();
                    httpHandler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
                    return httpHandler;
                });

            services
                .AddOpenTelemetryTracing((sp, cfg) =>
                {
                    var configuration = sp.GetRequiredService<IConfiguration>();
                    var resourceBuilder = ResourceBuilder
                    .CreateDefault()
                    .AddService(workerName);

                    cfg.SetResourceBuilder(resourceBuilder)
                    .AddSource(nameof(ObservableConsumer))
                    .AddHttpClientInstrumentation((options) =>
                    {
                        options.Filter = (f) =>
                        {
                            return f.RequestUri.ToString().Contains("_bulk") != true;
                        };
                    })
                    .AddGrpcClientInstrumentation()
                    .AddZipkinExporter(zipkinExporterOptions =>
                    {
                        zipkinExporterOptions.Endpoint = new Uri(configuration["Zipkin:Address"]?.ToString());
                    })
                    .AddConsoleExporter();
                });

            services
                .AddSingleton((serviceProvider) =>
                {
                    Rabbit consumer = new Rabbit(
                        serviceProvider.GetRequiredService<ILogger<Rabbit>>(),
                        serviceProvider.GetRequiredService<IConfiguration>(),
                        serviceProvider.GetRequiredService<IHttpClientFactory>(),
                        serviceProvider.GetRequiredService<Searcher.SearcherClient>(),
                        serviceProvider.GetRequiredService<MetricReporter>(),
                        agentName);
                    return consumer;
                });
        });
    }
}
