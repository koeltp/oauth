namespace OAuth.Contracts.Common;

public class ApiResponse<T>
{
    public int Code { get; set; } = 200;
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public static ApiResponse<T> Success(T data)
    {
        return new ApiResponse<T>
        {
            Code = 200,
            Message = "操作成功",
            Data = data
        };
    }

    public static ApiResponse<T> Success(string message, T data)
    {
        return new ApiResponse<T>
        {
            Code = 200,
            Message = message,
            Data = data
        };
    }

    public static ApiResponse<T> Error(int code, string message)
    {
        return new ApiResponse<T>
        {
            Code = code,
            Message = message
        };
    }

    public static ApiResponse<T> BadRequest(string message = "请求参数错误")
    {
        return Error(400, message);
    }

    public static ApiResponse<T> Unauthorized(string message = "未授权")
    {
        return Error(401, message);
    }

    public static ApiResponse<T> Forbidden(string message = "禁止访问")
    {
        return Error(403, message);
    }

    public static ApiResponse<T> NotFound(string message = "资源未找到")
    {
        return Error(404, message);
    }

    public static ApiResponse<T> InternalError(string message = "服务器内部错误")
    {
        return Error(500, message);
    }
}

public class ApiResponse : ApiResponse<object>
{
    public static new ApiResponse Success()
    {
        return new ApiResponse
        {
            Code = 200,
            Message = "操作成功"
        };
    }

    public static new ApiResponse Success(string message)
    {
        return new ApiResponse
        {
            Code = 200,
            Message = message
        };
    }
}
