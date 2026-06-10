namespace DocMap.Api.Logging;

/// <summary>
/// Thread-safe writer that appends log lines to a per-day file (<c>yyyy-MM-dd.txt</c>).
/// The date prefix makes the newest day's file sort to the top lexically.
/// </summary>
public sealed class FileLogWriter
{
    private readonly string _directory;
    private readonly object _lock = new();

    public FileLogWriter(string directory)
    {
        _directory = directory;
        Directory.CreateDirectory(_directory);
    }

    public void Write(string line)
    {
        var path = Path.Combine(_directory, $"{DateTime.UtcNow:yyyy-MM-dd}.txt");
        try
        {
            lock (_lock)
            {
                // FileShare.ReadWrite lets the admin viewer read and tolerates other
                // writers; the try/catch guarantees logging never breaks a request.
                using var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                using var writer = new StreamWriter(stream);
                writer.WriteLine(line);
            }
        }
        catch
        {
            // Best-effort logging — drop the line rather than fail the caller.
        }
    }
}
