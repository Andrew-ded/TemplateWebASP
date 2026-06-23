namespace Web.Errors;

public class ApiErrorResponse
{
    public ApiError Error { get; set; } = new();
}

public class ApiError
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public static class ApiErrors
{
    public static ApiErrorResponse Create(string code, string message) => new()
    {
        Error = new ApiError { Code = code, Message = message }
    };

    public static ApiErrorResponse NotFound(string message = "Resource not found")
        => Create("NOT_FOUND", message);

    public static ApiErrorResponse BadRequest(string message = "Invalid request")
        => Create("BAD_REQUEST", message);

    public static ApiErrorResponse Unauthorized(string message = "Authentication is required")
        => Create("UNAUTHORIZED", message);

    public static ApiErrorResponse Forbidden(string message = "You do not have access to this resource")
        => Create("FORBIDDEN", message);

    public static ApiErrorResponse RateLimitExceeded(string message = "Too many requests")
        => Create("RATE_LIMIT_EXCEEDED", message);
}
