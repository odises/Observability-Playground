using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace OpenTracing;

//Singleton instance
public class Bus
{
    private readonly ConcurrentDictionary<string, Func<string, string, Task>> callbacks = new();

    #region QUEUEING_STATIC_PROPS
    private readonly string SEARCH_COMMAND = "search";
    private readonly string SEARCH_QUEUE = typeof(Bus).Name + "_" + "search";
    private readonly string RESPOSNE_QUEUE = typeof(Bus).Name + "_" + "response";
    #endregion

    private readonly RabbitMQ.Client.IAsyncConnectionFactory connectionFactory;
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly ILogger<Bus> logger;
    private RabbitMQ.Client.IConnection connection;
    private RabbitMQ.Client.IModel rwChannel;

    public Bus(IConfiguration configuration, IHttpContextAccessor httpContextAccessor, ILogger<Bus> logger)
    {
        connectionFactory = new RabbitMQ.Client.ConnectionFactory()
        {
            AutomaticRecoveryEnabled = true,
            TopologyRecoveryEnabled = true,
            HostName = configuration["Rabbit:Host"]?.ToString(),
            Port = Convert.ToInt32(configuration["Rabbit:Port"]?.ToString()),
            UserName = configuration["Rabbit:Username"]?.ToString(),
            Password = configuration["Rabbit:Password"]?.ToString(),
            DispatchConsumersAsync = true
        };

        this.httpContextAccessor = httpContextAccessor;
        this.logger = logger;
    }

    public void Init()
    {
        connection = connectionFactory.CreateConnection();
        logger?.LogInformation("connection to bus instanciated");

        rwChannel = new ObservableChannel(logger, connection.CreateModel());
        logger?.LogInformation("channel instanciated");

        var consumer = new ObservableConsumer(logger, rwChannel, OnResponseReceived);

        rwChannel.QueueDeclare(RESPOSNE_QUEUE, true, false, false, null);
        logger?.LogInformation($"respose queue declared: {RESPOSNE_QUEUE}");

        rwChannel.ExchangeDeclare(SEARCH_COMMAND, "fanout", true, false, default);
        logger?.LogInformation($"request exchange declared: {SEARCH_COMMAND}");


        logger?.LogInformation($"going to consume on response queue: {RESPOSNE_QUEUE}");
        rwChannel.BasicConsume(RESPOSNE_QUEUE, true, "gateway-consumer", true, false, null, consumer);
    }

    public void Publish(string id, string query, Func<string, string, Task> callback)
    {
        callbacks.TryAdd(id, callback);

        var rabbitChannelProps = rwChannel.CreateBasicProperties();
        InjectTestingHeaders(rabbitChannelProps);

        rabbitChannelProps.CorrelationId = id;
        rabbitChannelProps.ReplyTo = RESPOSNE_QUEUE;

        logger?.LogInformation("going to publish request, correlationId: " + id);
        rwChannel.BasicPublish(SEARCH_COMMAND, "", false, rabbitChannelProps, System.Text.Encoding.UTF8.GetBytes(query));
    }

    ///
    /// This method will be excuted on each response.
    ///
    private async Task OnResponseReceived(object sender, BasicDeliverEventArgs @event)
    {
        // throw new Exception("fucking shit");
        string id = @event.BasicProperties.CorrelationId;
        string body = System.Text.Encoding.UTF8.GetString(@event.Body.ToArray());

        if (!string.IsNullOrWhiteSpace(body))
        {
            logger?.LogError($"response with correlationId: {id} received: " + body);
        }
        else
        {
            logger?.LogError($"empty response received through bus, correlationId: {id}");
        }

        if (callbacks.TryGetValue(id, out var callback))
        {
            logger?.LogInformation($"going to invoke callback after response deserializtion, correlationId: {id}");
            await callback?.Invoke(id, body);
        }
        else
        {
            logger?.LogError("no callback found for this response id: " + id);
        }

        logger?.LogInformation($"removing callback for memory optimization, correlationId: {id}");
        callbacks.TryRemove(id, out var removed);
    }

    private void InjectTestingHeaders(IBasicProperties props)
    {
        if (null == props) return;
        if (null == props.Headers)
            props.Headers = new Dictionary<string, object>();

        if (httpContextAccessor?.HttpContext?.Request?.Headers?.Count > 0)
        {
            foreach (var header in httpContextAccessor.HttpContext.Request.Headers)
            {
                if (header.Key.StartsWith("load") == true)
                {
                    props.Headers.Add(header.Key, header.Value.FirstOrDefault());
                }
            }
        }
    }
}