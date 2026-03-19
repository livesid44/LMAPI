namespace LMAPI.Models;

public class LogQueryResponse
{
    public IReadOnlyList<LogEntry> Entries { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
