using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

public class ObservableConsumer : AsyncEventingBasicConsumer
{
    private static readonly ActivitySource ActivitySource = new(nameof(ObservableConsumer), "1.0.0");
    private readonly IModel channel;
    private readonly Func<object, BasicDeliverEventArgs, Task> handleMessageRecieved;

    public ObservableConsumer(IModel channel, Func<object, BasicDeliverEventArgs, Task> handleMessageRecieved) : base(channel)
    {
        this.Received += MessageReceived;
        this.channel = channel;
        this.handleMessageRecieved = handleMessageRecieved;
    }

    private async Task MessageReceived(object sender, BasicDeliverEventArgs @event)
    {
        TextMapPropagator propagator = new TraceContextPropagator();

        var parentContext = propagator.Extract(default, @event.BasicProperties, ExtractTraceContextFromBasicProperties);
        Baggage.Current = parentContext.Baggage;

        using (System.Diagnostics.Activity activity = ActivitySource.StartActivity($"Sub|{@event.RoutingKey}", System.Diagnostics.ActivityKind.Consumer, parentContext.ActivityContext))
        {
            activity.AddEvent(new System.Diagnostics.ActivityEvent("worker-query-sub"));
            await WaitIfNeeded(@event.BasicProperties);
            await handleMessageRecieved.Invoke(sender, @event);
            channel.BasicAck(@event.DeliveryTag, false);
            activity.Stop();
        }
    }

    private async Task WaitIfNeeded(IBasicProperties basicProperties)
    {
        if (basicProperties?.IsHeadersPresent() != true) return;

        foreach (var item in basicProperties.Headers)
        {
            if (item.Key.StartsWith("load") && item.Key.Contains("worker"))
            {
                try
                {
                    int sleep = Convert.ToInt32(Encoding.UTF8.GetString((byte[])item.Value));
                    await Task.Delay(sleep);
                    break;
                }
                catch { }

            }
        }
    }

    private static IEnumerable<string> ExtractTraceContextFromBasicProperties(IBasicProperties props, string key)
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
            Console.WriteLine(ex);
        }

        return Enumerable.Empty<string>();
    }
}