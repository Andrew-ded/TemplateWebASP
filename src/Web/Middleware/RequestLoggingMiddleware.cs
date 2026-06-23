using System.Diagnostics;
using System.Text;
using Serilog;
using Serilog.Context;
using ILogger = Serilog.ILogger;

namespace Web.Middleware;

public class RequestLoggingMiddleware(RequestDelegate next)
{
    private static readonly ILogger _log = Log.ForContext("SourceContext", "Api.Requests");

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_log.IsEnabled(Serilog.Events.LogEventLevel.Debug))
        {
            await next(context);
            return;
        }

        var request = context.Request;
        var sw = Stopwatch.StartNew();

        var requestBody = await ReadRequestBodyAsync(request);

        var headers = FormatHeaders(request.Headers);

        _log.Debug(
            ">>> {Method} {Scheme}://{Host}{Path}{QueryString}\nHeaders:\n{Headers}\nBody:\n{RequestBody}",
            request.Method,
            request.Scheme,
            request.Host,
            request.Path,
            request.QueryString,
            headers,
            requestBody);

        var originalBodyStream = context.Response.Body;
        using var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        try
        {
            await next(context);
        }
        finally
        {
            sw.Stop();

            responseBodyStream.Seek(0, SeekOrigin.Begin);
            var responseBody = await new StreamReader(responseBodyStream).ReadToEndAsync();

            var responseHeaders = FormatHeaders(context.Response.Headers);

            using (LogContext.PushProperty("StatusCode", context.Response.StatusCode))
            {
                _log.Debug(
                    "<<< {Method} {Path} -> {StatusCode} ({Elapsed}ms)\nResponse Headers:\n{ResponseHeaders}\nResponse Body:\n{ResponseBody}",
                    request.Method,
                    request.Path,
                    context.Response.StatusCode,
                    sw.ElapsedMilliseconds,
                    responseHeaders,
                    responseBody);
            }

            responseBodyStream.Seek(0, SeekOrigin.Begin);
            await responseBodyStream.CopyToAsync(originalBodyStream);
            context.Response.Body = originalBodyStream;
        }
    }

    private static async Task<string> ReadRequestBodyAsync(HttpRequest request)
    {
        if (request.ContentLength is null or 0)
            return "(empty)";

        request.EnableBuffering();
        request.Body.Position = 0;

        using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();

        request.Body.Position = 0;
        return string.IsNullOrWhiteSpace(body) ? "(empty)" : body;
    }

    private static string FormatHeaders(IHeaderDictionary headers)
    {
        var sb = new StringBuilder();
        foreach (var (key, value) in headers)
        {
            var displayValue = key.Equals("Authorization", StringComparison.OrdinalIgnoreCase)
                ? "***"
                : value.ToString();
            sb.AppendLine($"  {key}: {displayValue}");
        }
        return sb.Length > 0 ? sb.ToString().TrimEnd() : "(none)";
    }
}
