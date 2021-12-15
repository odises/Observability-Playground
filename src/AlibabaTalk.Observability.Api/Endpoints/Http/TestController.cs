using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace OpenTracing;

public class TestController : Controller
{
    private readonly int TIMEOUT_MILISECONDS = 10000;

    [HttpGet]
    [Route("search")]
    public async Task<IActionResult> Test([FromQuery] string query, [FromServices] SearchService searchService)
    {
        string[] queries = query?.Split("|")?.Select(x => x?.Trim())?.Where(x => !string.IsNullOrEmpty(x))?.ToArray();


        foreach (var queryItem in queries)
        {
            if (!string.IsNullOrWhiteSpace(queryItem))
            {
                searchService.Queue(queryItem);
            }
        }

        var readChannel = searchService.RunQuery();

        System.Text.StringBuilder responseBuilder = new System.Text.StringBuilder();
        System.Threading.CancellationTokenSource cancellationTokenSource = new System.Threading.CancellationTokenSource();

        _ = Task.Run(async () =>
         {
             await Task.Delay(TIMEOUT_MILISECONDS);
             cancellationTokenSource.Cancel();
         });


        await foreach (var singleResponse in readChannel.ReadAllAsync(cancellationTokenSource.Token))
        {
            responseBuilder.AppendLine(singleResponse);
        }

        return Ok(responseBuilder.ToString());
    }
}