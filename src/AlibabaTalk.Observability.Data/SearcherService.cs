using Grpc.Core;

public class SearcherService : Searcher.SearcherBase
{
    private readonly ILogger<SearcherService> _logger;

    public SearcherService(ILogger<SearcherService> logger)
    {
        _logger = logger;
    }

    public override async Task<SearchResponse> Search(SearchRequest request,
        ServerCallContext context)
    {
        _logger.LogInformation("Saying hello to {Id}", request.Id);

        return await Task.FromResult(new SearchResponse()
        {
            Content = "Response: " + request.Id
        });
    }
}