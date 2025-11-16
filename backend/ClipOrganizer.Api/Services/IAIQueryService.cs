namespace ClipOrganizer.Api.Services;

public interface IAIQueryService
{
    Task<QueryParseResult> ParseQueryAsync(string userQuery, QueryContext context);
}

public class QueryContext
{
    public List<AvailableTag> AvailableTags { get; set; } = new();
    public List<string> AvailableSubfolders { get; set; } = new();
}

public class QueryParseResult
{
    public string? SearchTerm { get; set; }
    public List<int> TagIds { get; set; } = new();
    public List<string> Subfolders { get; set; } = new();
    public string? SortBy { get; set; }
    public string? SortOrder { get; set; }
    public bool UnclassifiedOnly { get; set; }
    public bool FavoriteOnly { get; set; }
    public string? InterpretedQuery { get; set; }
}

