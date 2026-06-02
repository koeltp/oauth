using System.Text.Json;
using Taipi.Core.RQRS;

namespace OAuth.Server.Middlewares;

public class GlobalExceptionHandler
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(RequestDelegate next, ILogger<GlobalExceptionHandler> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发生未处理的异常");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        // OAuth endpoints (OpenIddict /connect/*) should return OAuth standard error format
        if (context.Request.Path.StartsWithSegments("/connect"))
        {
            return HandleOAuthExceptionAsync(context, exception);
        }

        int code;
        string message;

        switch (exception)
        {
            case UnauthorizedAccessException:
                code = 401;
                message = "未授权访问";
                break;

            case ArgumentException argEx:
                code = 400;
                message = argEx.Message;
                break;

            case KeyNotFoundException:
                code = 404;
                message = "资源未找到";
                break;

            case Microsoft.EntityFrameworkCore.DbUpdateException:
                code = 409;
                message = "数据库操作失败，请稍后重试";
                break;

            case FormatException:
                code = 400;
                message = "输入格式无效，请检查输入";
                break;

            case InvalidOperationException:
                if (exception.Message.Contains("用户不存在"))
                {
                    code = 401;
                    message = "用户不存在，请重新登录";
                }
                else
                {
                    code = 400;
                    message = exception.Message;
                }
                break;

            case NotSupportedException:
            case System.Text.Json.JsonException:
                code = 400;
                message = "请求数据格式错误，请检查输入";
                break;

            default:
                code = 500;
                message = "服务器内部错误";
                break;
        }

        var response = new StatusResponseResult
        {
            Code = code,
            Message = message,
            Timestamp = DateTime.UtcNow
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }

    private static Task HandleOAuthExceptionAsync(HttpContext context, Exception exception)
    {
        string error;
        string description;

        switch (exception)
        {
            case UnauthorizedAccessException:
                error = "access_denied";
                description = "访问被拒绝";
                break;

            case ArgumentException:
                error = "invalid_request";
                description = exception.Message;
                break;

            case KeyNotFoundException:
                error = "invalid_request";
                description = "请求的资源未找到";
                break;

            default:
                error = "server_error";
                description = "服务器内部错误";
                break;
        }

        var response = new { error, error_description = description };

        return context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}

public static class GlobalExceptionHandlerExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        return app.UseMiddleware<GlobalExceptionHandler>();
    }
}