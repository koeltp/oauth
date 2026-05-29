namespace OAuth.Contracts.Common;

public class ApiResponse<T>
{
    public int Code { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public static ApiResponse<T> Success(T data)
    {
        return new ApiResponse<T>
        {
            Code = 200,
            Message = "Success",
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

    public static ApiResponse<T> BadRequest(string message)
    {
        return Error(400, message);
    }

    public static ApiResponse<T> Unauthorized(string message)
    {
        return Error(401, message);
    }

    public static ApiResponse<T> Forbidden(string message)
    {
        return Error(403, message);
    }

    public static ApiResponse<T> NotFound(string message)
    {
        return Error(404, message);
    }

    public static ApiResponse<T> InternalError(string message)
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
            Message = "Success"
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
