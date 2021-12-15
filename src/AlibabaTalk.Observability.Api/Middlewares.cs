using System;
using Microsoft.AspNetCore.Builder;

public static class Middlewares
{
    public static void UseTraceIdentifierOnResponse(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            context.Response.Headers.Add("traceId", System.Diagnostics.Activity.Current.Id);
            await next();
        });
    }

    public static void UseLoadTestMiddleware(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            var request = context.Request;
            int sleepBefore = 0;
            int sleepAfter = 0;

            foreach (var header in request.Headers)
            {
                if (header.Key.StartsWith("load") && header.Key.Contains("gateway") && header.Key.Contains("before"))
                {
                    try
                    {
                        sleepBefore = Convert.ToInt32(header.Value);
                    }
                    catch { }
                }

                if (header.Key.StartsWith("load") && header.Key.Contains("gateway") && header.Key.Contains("after"))
                {
                    try
                    {
                        sleepAfter = Convert.ToInt32(header.Value);
                    }
                    catch { }
                }
            }

            if (sleepBefore > 0)
            {
                await System.Threading.Tasks.Task.Delay(sleepBefore);
            }

            await next();

            if (sleepAfter > 0)
            {
                await System.Threading.Tasks.Task.Delay(sleepAfter);
            }
        });
    }
}