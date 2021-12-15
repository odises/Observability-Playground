using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Context.Propagation;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

public class ObservableChannel : IModel
{
    private readonly ILogger logger;
    private readonly IModel channel;

    public ObservableChannel(ILogger logger, IModel channel)
    {
        this.logger = logger;
        this.channel = channel;
    }
    public int ChannelNumber => channel.ChannelNumber;
    public ShutdownEventArgs CloseReason => channel.CloseReason;

    public IBasicConsumer DefaultConsumer { get => channel.DefaultConsumer; set { channel.DefaultConsumer = value; } }

    public bool IsClosed => channel.IsClosed;

    public bool IsOpen => channel.IsOpen;

    public ulong NextPublishSeqNo => channel.NextPublishSeqNo;

    public TimeSpan ContinuationTimeout
    {
        get
        {
            return channel.ContinuationTimeout;
        }

        set
        {
            channel.ContinuationTimeout = value;
        }
    }

#pragma warning disable 0067

    public event EventHandler<BasicAckEventArgs> BasicAcks;
    public event EventHandler<BasicNackEventArgs> BasicNacks;
    public event EventHandler<EventArgs> BasicRecoverOk;
    public event EventHandler<BasicReturnEventArgs> BasicReturn;
    public event EventHandler<CallbackExceptionEventArgs> CallbackException;
    public event EventHandler<FlowControlEventArgs> FlowControl;
    public event EventHandler<ShutdownEventArgs> ModelShutdown;
#pragma warning  restore 0067

    public void Abort()
    {
        channel.Abort();
    }

    public void Abort(ushort replyCode, string replyText)
    {
        channel.Abort(replyCode, replyText);
    }

    public void BasicAck(ulong deliveryTag, bool multiple)
    {
        channel.BasicAck(deliveryTag, multiple);
    }

    public void BasicCancel(string consumerTag)
    {
        channel.BasicCancel(consumerTag);
    }

    public void BasicCancelNoWait(string consumerTag)
    {
        channel.BasicCancelNoWait(consumerTag);
    }

    public string BasicConsume(string queue, bool autoAck, string consumerTag, bool noLocal, bool exclusive, IDictionary<string, object> arguments, IBasicConsumer consumer)
    {
        return channel.BasicConsume(queue, autoAck, consumerTag, noLocal, exclusive, arguments, consumer);
    }

    public BasicGetResult BasicGet(string queue, bool autoAck)
    {
        return channel.BasicGet(queue, autoAck);
    }

    public void BasicNack(ulong deliveryTag, bool multiple, bool requeue)
    {
        channel.BasicNack(deliveryTag, multiple, requeue);
    }

    public void BasicPublish(string exchange, string routingKey, bool mandatory, IBasicProperties basicProperties, ReadOnlyMemory<byte> body)
    {
        var activitySource = System.Diagnostics.Activity.Current.Source;

        TextMapPropagator propagator = new TraceContextPropagator();
        propagator.Inject(new PropagationContext(System.Diagnostics.Activity.Current.Context, OpenTelemetry.Baggage.Current), basicProperties, InjectContextIntoHeader);


        using (System.Diagnostics.Activity activity = activitySource.StartActivity($"Pub:{routingKey}", System.Diagnostics.ActivityKind.Producer, System.Diagnostics.Activity.Current.Context))
        {
            activity.AddEvent(new System.Diagnostics.ActivityEvent("worker-response-pub"));
            channel.BasicPublish(exchange, routingKey, mandatory, basicProperties, body);
            activity.Stop();
        }
    }

    public void BasicQos(uint prefetchSize, ushort prefetchCount, bool global)
    {
        channel.BasicQos(prefetchSize, prefetchCount, global);
    }

    public void BasicRecover(bool requeue)
    {
        channel.BasicRecover(requeue);
    }

    public void BasicRecoverAsync(bool requeue)
    {
        channel.BasicRecoverAsync(requeue);
    }

    public void BasicReject(ulong deliveryTag, bool requeue)
    {
        channel.BasicReject(deliveryTag, requeue);
    }

    public void Close()
    {
        channel.Close();
    }

    public void Close(ushort replyCode, string replyText)
    {
        channel.Close(replyCode, replyText);
    }

    public void ConfirmSelect()
    {
        channel.ConfirmSelect();
    }

    public uint ConsumerCount(string queue)
    {
        return channel.ConsumerCount(queue);
    }

    public IBasicProperties CreateBasicProperties()
    {
        return channel.CreateBasicProperties();
    }

    public IBasicPublishBatch CreateBasicPublishBatch()
    {
        return channel.CreateBasicPublishBatch();
    }

    public void Dispose()
    {
        channel.Dispose();
    }

    public void ExchangeBind(string destination, string source, string routingKey, IDictionary<string, object> arguments)
    {
        channel.ExchangeBind(destination, source, routingKey, arguments);
    }

    public void ExchangeBindNoWait(string destination, string source, string routingKey, IDictionary<string, object> arguments)
    {
        channel.ExchangeBindNoWait(destination, source, routingKey, arguments);
    }

    public void ExchangeDeclare(string exchange, string type, bool durable, bool autoDelete, IDictionary<string, object> arguments)
    {
        channel.ExchangeDeclare(exchange, type, durable, autoDelete, arguments);
    }

    public void ExchangeDeclareNoWait(string exchange, string type, bool durable, bool autoDelete, IDictionary<string, object> arguments)
    {
        channel.ExchangeDeclareNoWait(exchange, type, durable, autoDelete, arguments);
    }

    public void ExchangeDeclarePassive(string exchange)
    {
        channel.ExchangeDeclarePassive(exchange);
    }

    public void ExchangeDelete(string exchange, bool ifUnused)
    {
        channel.ExchangeDelete(exchange, ifUnused);
    }

    public void ExchangeDeleteNoWait(string exchange, bool ifUnused)
    {
        channel.ExchangeDeleteNoWait(exchange, ifUnused);
    }

    public void ExchangeUnbind(string destination, string source, string routingKey, IDictionary<string, object> arguments)
    {
        channel.ExchangeUnbind(destination, source, routingKey, arguments);
    }

    public void ExchangeUnbindNoWait(string destination, string source, string routingKey, IDictionary<string, object> arguments)
    {
        channel.ExchangeUnbindNoWait(destination, source, routingKey, arguments);
    }

    public uint MessageCount(string queue)
    {
        return channel.MessageCount(queue);
    }

    public void QueueBind(string queue, string exchange, string routingKey, IDictionary<string, object> arguments)
    {
        channel.QueueBind(queue, exchange, routingKey, arguments);
    }

    public void QueueBindNoWait(string queue, string exchange, string routingKey, IDictionary<string, object> arguments)
    {
        channel.QueueBindNoWait(queue, exchange, routingKey, arguments);
    }

    public QueueDeclareOk QueueDeclare(string queue, bool durable, bool exclusive, bool autoDelete, IDictionary<string, object> arguments)
    {
        return channel.QueueDeclare(queue, durable, exclusive, autoDelete, arguments);
    }

    public void QueueDeclareNoWait(string queue, bool durable, bool exclusive, bool autoDelete, IDictionary<string, object> arguments)
    {
        channel.QueueDeclareNoWait(queue, durable, exclusive, autoDelete, arguments);
    }

    public QueueDeclareOk QueueDeclarePassive(string queue)
    {
        return channel.QueueDeclarePassive(queue);
    }

    public uint QueueDelete(string queue, bool ifUnused, bool ifEmpty)
    {
        return channel.QueueDelete(queue, ifUnused, ifEmpty);
    }

    public void QueueDeleteNoWait(string queue, bool ifUnused, bool ifEmpty)
    {
        channel.QueueDeleteNoWait(queue, ifUnused, ifEmpty);
    }

    public uint QueuePurge(string queue)
    {
        return channel.QueuePurge(queue);
    }

    public void QueueUnbind(string queue, string exchange, string routingKey, IDictionary<string, object> arguments)
    {
        channel.QueueUnbind(queue, exchange, routingKey, arguments);
    }

    public void TxCommit()
    {
        channel.TxCommit();
    }

    public void TxRollback()
    {
        channel.TxRollback();
    }

    public void TxSelect()
    {
        channel.TxSelect();
    }

    public bool WaitForConfirms()
    {
        return channel.WaitForConfirms();
    }

    public bool WaitForConfirms(TimeSpan timeout)
    {
        return channel.WaitForConfirms(timeout);
    }

    public bool WaitForConfirms(TimeSpan timeout, out bool timedOut)
    {
        return channel.WaitForConfirms(timeout, out timedOut);
    }

    public void WaitForConfirmsOrDie()
    {
        channel.WaitForConfirmsOrDie();
    }

    public void WaitForConfirmsOrDie(TimeSpan timeout)
    {
        channel.WaitForConfirmsOrDie(timeout);
    }

    private void InjectContextIntoHeader(IBasicProperties props, string key, string value)
    {
        try
        {
            props.Headers ??= new Dictionary<string, object>();
            props.Headers[key] = value;
        }
        catch (Exception ex)
        {
            this.logger?.LogError(ex, ex.Message);
        }
    }
}