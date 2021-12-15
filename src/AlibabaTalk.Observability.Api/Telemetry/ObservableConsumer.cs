using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

public class ObservableConsumer : AsyncEventingBasicConsumer
{
    private static readonly ActivitySource ActivitySource = new(nameof(ObservableConsumer));
    private readonly ILogger logger;
    private readonly IModel channel;
    private readonly Func<object, BasicDeliverEventArgs, Task> handleMessageRecieved;

    public ObservableConsumer(ILogger logger, IModel channel, Func<object, BasicDeliverEventArgs, Task> handleMessageRecieved) : base(channel)
    {
        this.Received += MessageReceived;
        this.logger = logger;
        this.channel = channel;
        this.handleMessageRecieved = handleMessageRecieved;
    }

    private async Task MessageReceived(object sender, BasicDeliverEventArgs @event)
    {
        TextMapPropagator propagator = new TraceContextPropagator();

        var parentContext = propagator.Extract(default, @event.BasicProperties, ExtractTraceContextFromBasicProperties);
        Baggage.Current = parentContext.Baggage;


        using (System.Diagnostics.Activity activity = ActivitySource.StartActivity($"Sub:{@event.RoutingKey}", System.Diagnostics.ActivityKind.Consumer, parentContext.ActivityContext))
        {
            try
            {
                await handleMessageRecieved.Invoke(sender, @event);
            }
            catch (System.Exception ex)
            {
                activity.AddTag("stack-trace", ex.ToString());
                activity.AddTag("error", ex.Message);
                activity.AddTag("otel.status_code", "ERROR");
                activity.AddEvent(new ActivityEvent("exception"));
                logger?.LogError(ex, ex.Message);
            }
            finally
            {
                activity.Stop();
            }
        }
    }

    private IEnumerable<string> ExtractTraceContextFromBasicProperties(IBasicProperties props, string key)
    {
        try
        {
            if (props.Headers.TryGetValue(key, out var value))
            {
                var bytes = value as byte[];
                return new[] { Encoding.UTF8.GetString(bytes) };
            }
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, ex.Message);
        }

        return Enumerable.Empty<string>();
    }
}