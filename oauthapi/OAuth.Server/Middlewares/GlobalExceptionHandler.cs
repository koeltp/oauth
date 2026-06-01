using System.Net;
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

        string message;

        switch (exception)
        {
            case UnauthorizedAccessException:
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                message = "未授权访问";
                break;

            case ArgumentException argEx:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                message = argEx.Message;
                break;

            case KeyNotFoundException:
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                message = "资源未找到";
                break;

            case Microsoft.EntityFrameworkCore.DbUpdateException:
                context.Response.StatusCode = (int)HttpStatusCode.Conflict;
                message = "数据库操作失败，请稍后重试";
                break;

            case FormatException:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                message = "输入格式无效，请检查输入";
                break;

            case InvalidOperationException:
                if (exception.Message.Contains("用户不存在"))
                {
                    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    message = "用户不存在，请重新登录";
                }
                else
                {
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    message = exception.Message;
                }
                break;

            case NotSupportedException:
            case System.Text.Json.JsonException:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                message = "请求数据格式错误，请检查输入";
                break;

            default:
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                message = "服务器内部错误";
                break;
        }

        var response = new StatusResponseResult
        {
            Code = context.Response.StatusCode,
            Message = message,
            Timestamp = DateTime.UtcNow
        };

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