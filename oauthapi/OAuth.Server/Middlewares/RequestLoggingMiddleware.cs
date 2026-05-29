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
        // Log request details
        _logger.LogInformation("====== REQUEST START ======");
        _logger.LogInformation($"Method: {context.Request.Method}");
        _logger.LogInformation($"Path: {context.Request.Path}");
        _logger.LogInformation($"QueryString: {context.Request.QueryString}");
        
        // Log headers
        var headers = string.Join(", ", context.Request.Headers.Select(h => $"{h.Key}: {h.Value}"));
        _logger.LogInformation($"Headers: {headers}");
        
        // Log body (only for POST/PUT requests)
        if (context.Request.ContentLength > 0 && 
            (context.Request.Method == "POST" || context.Request.Method == "PUT"))
        {
            context.Request.EnableBuffering();
            var body = await new StreamReader(context.Request.Body, Encoding.UTF8).ReadToEndAsync();
            context.Request.Body.Position = 0; // Reset position for downstream middleware
            _logger.LogInformation($"Body: {body}");
        }
        
        _logger.LogInformation("------------------------");

        // Call the next middleware in the pipeline
        await _next(context);

        // Log response details
        _logger.LogInformation($"Response Status: {context.Response.StatusCode}");
        _logger.LogInformation("====== REQUEST END ======");
    }
}

public static class RequestLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RequestLoggingMiddleware>();
    }
}
