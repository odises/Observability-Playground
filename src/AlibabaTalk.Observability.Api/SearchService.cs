
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Channels;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using OpenTracing;

public class SearchService
{
    private readonly object dictionaryLock = new();
    private Dictionary<string, string> queries = new();
    Channel<string> channel;
    private readonly IConfiguration config;
    private readonly OpenTracing.Bus bus;

    public SearchService(IConfiguration config, OpenTracing.Bus bus)
    {
        channel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions()
        {
            SingleReader = true
        });
        this.config = config;
        this.bus = bus;
    }

    public void Queue(string item)
    {
        string id = Guid.NewGuid().ToString().Replace("-", "");
        lock (dictionaryLock)
        {
            queries.Add(id, item);
        }

    }

    public ChannelReader<string> RunQuery()
    {
        foreach (var query in queries)
        {
            bus.Publish(query.Key, query.Value, async (id, response) =>
            {
                InjectTestExceptions(response);

                bool shouldClose = false;
                lock (dictionaryLock)
                {
                    queries.Remove(id);
                    shouldClose = queries.Count == 0;
                }

                await channel.Writer.WriteAsync(response);
                if (shouldClose)
                {
                    channel.Writer.Complete();
                }
            });
        }

        return channel.Reader;
    }

    private void InjectTestExceptions(string maybeBuggyResponse)
    {
        List<string> buggyNumbers = config["Dummy:Buggy"]?.ToString()?.Split(",")?.Select(x => x?.Trim())?.Where(x => !string.IsNullOrWhiteSpace(x))?.ToList();
        foreach (var buggyNumber in buggyNumbers)
        {
            if (maybeBuggyResponse.Contains(buggyNumber))
            {
                throw new InvalidProgramException("invalid input data: " + buggyNumber);
            }
        }
    }
}