using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public static class ApplicationExtensions
{
    public static void RegisterStartupEvents(this WebApplication app, string workerName)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();

        lifetime.ApplicationStarted.Register(async () =>
        {
            logger?.LogInformation("worker startup: " + workerName);
            while (true)
            {
                try
                {
                    app.Services.GetRequiredService<Rabbit>().Init();
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