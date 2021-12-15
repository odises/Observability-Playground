using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTracing;

public static class ApplicationExtensions
{
    public static void RegisterStartupEvents(this WebApplication app)
    {
        var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
        lifetime.ApplicationStarted.Register(async () =>
        {
            var logger = app.Services.GetRequiredService<ILogger<Program>>();

            while (true)
            {
                try
                {
                    app.Services.GetRequiredService<Bus>().Init();
                    break;
                }
                catch (System.Exception ex)
                {
                    logger?.LogError(ex, ex.Message);
                    logger?.LogError("error connecting to rabbit, retrying in 5 seconds");

                    await System.Threading.Tasks.Task.Delay(5000);
                    continue;
                }
            }
        });
    }
}