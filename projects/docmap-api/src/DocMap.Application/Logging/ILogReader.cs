using DocMap.Application.Responses;

namespace DocMap.Application.Logging;

public interface ILogReader
{
    /// <summary>Available log dates (file names without extension), newest first.</summary>
    IEnumerable<string> GetAvailableDates();

    /// <summary>Parses, filters and paginates one day's log file, newest entries first.</summary>
    PagedResponse<LogEntryDto> Query(LogQuery query);
}
