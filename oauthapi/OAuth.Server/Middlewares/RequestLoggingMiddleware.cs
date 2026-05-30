using System.Text;

namespace OAuth.Server.Middlewares;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var sb = new StringBuilder();
        sb.AppendLine("====== REQUEST START ======");
        sb.AppendLine($"Method: {context.Request.Method}");
        sb.AppendLine($"Path: {context.Request.Path}");
        sb.AppendLine($"QueryString: {context.Request.QueryString}");

        var headers = string.Join(", ", context.Request.Headers.Select(h => $"{h.Key}: {h.Value}"));
        sb.AppendLine($"Headers: {headers}");

        if (context.Request.ContentLength > 0 &&
            (context.Request.Method == "POST" || context.Request.Method == "PUT"))
        {
            context.Request.EnableBuffering();
            var body = await new StreamReader(context.Request.Body, Encoding.UTF8).ReadToEndAsync();
            context.Request.Body.Position = 0;
            sb.AppendLine($"Body: {body}");
        }

        sb.AppendLine("------------------------");
        _logger.LogInformation(sb.ToString());

        await _next(context);

        _logger.LogInformation($"====== REQUEST END ====== Status: {context.Response.StatusCode}");
    }
}

public static class RequestLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RequestLoggingMiddleware>();
    }
}
