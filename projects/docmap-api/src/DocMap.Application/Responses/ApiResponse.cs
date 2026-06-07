namespace DocMap.Application.Responses;

public record ApiResponse<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string? Message { get; init; }
    public IEnumerable<string>? Errors { get; init; }

    private ApiResponse(bool success, T? data, string? message, IEnumerable<string>? errors)
    {
        Success = success;
        Data = data;
        Message = message;
        Errors = errors;
    }

    public static ApiResponse<T> Ok(T data, string? message = null) =>
        new(true, data, message, null);

    public static ApiResponse<T> Created(T data) =>
        new(true, data, "Resource created successfully.", null);

    public static ApiResponse<T> Fail(string message, IEnumerable<string>? errors = null) =>
        new(false, default, message, errors);
}
