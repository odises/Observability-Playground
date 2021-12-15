using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;

using AlibabaTalk.Observability.Consumer;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using RabbitMQ.Client.Events;

using static Searcher;

public class Rabbit
{
    private readonly string SEARCH_COMMAND = "search";
    private readonly string SEARCH_QUEUE = typeof(Rabbit).Name + "_" + "search";
    private readonly RabbitMQ.Client.IAsyncConnectionFactory connectionFactory;
    private readonly ILogger<Rabbit> logger;
    private readonly IConfiguration configuration;
    private readonly IHttpClientFactory httpClientFactory;
    private readonly SearcherClient searcherClient;
    private RabbitMQ.Client.IConnection connection;
    private readonly MetricReporter _metricReporter;

    public RabbitMQ.Client.IModel rabbitChannel { get; private set; }

    public Rabbit(ILogger<Rabbit> logger,
                  IConfiguration configuration,
                  IHttpClientFactory httpClientFactory,
                  SearcherClient searcherClient,
                  MetricReporter metricReporter,
                  string clientName = "")
    {
        connectionFactory = new RabbitMQ.Client.ConnectionFactory()
        {
            ClientProvidedName = clientName,
            AutomaticRecoveryEnabled = true,
            TopologyRecoveryEnabled = true,
            HostName = configuration["Rabbit:Host"]?.ToString(),
            Port = Convert.ToInt32(configuration["Rabbit:Port"]?.ToString()),
            UserName = configuration["Rabbit:Username"]?.ToString(),
            Password = configuration["Rabbit:Password"]?.ToString(),
            DispatchConsumersAsync = true
        };

        this.logger = logger;
        this.configuration = configuration;
        this.httpClientFactory = httpClientFactory;
        this.searcherClient = searcherClient;
        _metricReporter = metricReporter;
    }

    public void Init()
    {
        ushort prefetch;
        if (!ushort.TryParse(configuration["Rabbit:Prefetch"]?.ToString(), out prefetch))
        {
            prefetch = 1;
        }

        connection = connectionFactory.CreateConnection();
        rabbitChannel = new ObservableChannel(logger, connection.CreateModel());

        rabbitChannel.ExchangeDeclare(SEARCH_COMMAND, "fanout", true, false, default);
        rabbitChannel.QueueDeclare(SEARCH_QUEUE, true, false, false, default);

        rabbitChannel.QueueBind(SEARCH_QUEUE, SEARCH_COMMAND, "#", default);

        rabbitChannel.BasicQos(0, prefetch, true);
        rabbitChannel.BasicConsume(queue: SEARCH_QUEUE,
                          autoAck: false,
                          noLocal: true,
                          exclusive: false,
                          arguments: default,
                          consumer: new ObservableConsumer(rabbitChannel, logger, HandleMessageRecieved),
                          consumerTag: "consumer");
    }

    private async Task HandleMessageRecieved(object sender, BasicDeliverEventArgs eventArgs)
    {
        string query = System.Text.Encoding.UTF8.GetString(eventArgs.Body.ToArray());

        var props = rabbitChannel.CreateBasicProperties();
        props.CorrelationId = eventArgs.BasicProperties.CorrelationId;

        var stopWatch = Stopwatch.StartNew();
        bool isSuccessful = false;

        try
        {
            SearchResponse response = await searcherClient.SearchAsync(
                new SearchRequest()
                {
                    Id = Convert.ToInt32(query)
                });

            isSuccessful = true;
            logger?.LogInformation($"going to publish respnose of request{props.CorrelationId}: {response.Content}");
            rabbitChannel.BasicPublish("", eventArgs.BasicProperties.ReplyTo, true, props, System.Text.Encoding.UTF8.GetBytes(response.Content));
        }
        catch (Exception ex)
        {
            rabbitChannel.BasicPublish("", eventArgs.BasicProperties.ReplyTo, true, props, System.Text.Encoding.UTF8.GetBytes("error"));
            logger?.LogError(ex.Message, ex);
        }
        finally
        {
            stopWatch.Stop();
            _metricReporter.RegisterResponseTime(isSuccessful, stopWatch.Elapsed);
        }
    }
}