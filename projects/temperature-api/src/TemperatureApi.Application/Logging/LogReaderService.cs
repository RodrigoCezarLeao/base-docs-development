using System.Text.RegularExpressions;
using TemperatureApi.Application.Responses;

namespace TemperatureApi.Application.Logging;

public sealed partial class LogReaderService(LogReaderOptions options) : ILogReader
{
    public IEnumerable<string> GetAvailableDates()
    {
        if (!Directory.Exists(options.Directory)) return [];
        return Directory.EnumerateFiles(options.Directory, "*.txt")
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => name is not null && DatePattern().IsMatch(name))
            .Select(name => name!)
            .OrderByDescending(d => d, StringComparer.Ordinal)
            .ToList();
    }

    public PagedResponse<LogEntryDto> Query(LogQuery query)
    {
        var date = string.IsNullOrWhiteSpace(query.Date)
            ? DateTime.UtcNow.ToString("yyyy-MM-dd")
            : query.Date;
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize is < 1 or > 500 ? 50 : query.PageSize;

        // Path-traversal guard: only a strict yyyy-MM-dd date is ever turned into a file name.
        if (!DatePattern().IsMatch(date))
            return new PagedResponse<LogEntryDto>([], 0, page, pageSize);

        var path = Path.Combine(options.Directory, $"{date}.txt");
        if (!File.Exists(path))
            return new PagedResponse<LogEntryDto>([], 0, page, pageSize);

        var entries = File.ReadLines(path)
            .Select(Parse)
            .Where(e => e is not null)
            .Select(e => e!)
            .Where(e => Matches(e, query))
            .Reverse() // newest first (file is appended oldest → newest)
            .ToList();

        var items = entries.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return new PagedResponse<LogEntryDto>(items, entries.Count, page, pageSize);
    }

    private static LogEntryDto? Parse(string line)
    {
        var parts = line.Split(" | ", 6, StringSplitOptions.None);
        return parts.Length < 6 ? null : new LogEntryDto(parts[0], parts[1], parts[2], parts[3], parts[4], parts[5]);
    }

    private static bool Matches(LogEntryDto e, LogQuery q)
    {
        if (!string.IsNullOrWhiteSpace(q.Level) && !string.Equals(e.Level, q.Level, StringComparison.OrdinalIgnoreCase)) return false;
        if (!string.IsNullOrWhiteSpace(q.Category) && !e.Category.Contains(q.Category, StringComparison.OrdinalIgnoreCase)) return false;
        if (!string.IsNullOrWhiteSpace(q.UserId) && !string.Equals(e.UserId, q.UserId, StringComparison.OrdinalIgnoreCase)) return false;
        if (!string.IsNullOrWhiteSpace(q.Q) && !e.Message.Contains(q.Q, StringComparison.OrdinalIgnoreCase)) return false;
        return true;
    }

    [GeneratedRegex(@"^\d{4}-\d{2}-\d{2}$")]
    private static partial Regex DatePattern();
}
