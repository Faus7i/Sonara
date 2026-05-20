namespace MusicRec.Shared;

/// <summary>
/// 统一 API 响应格式 — 所有接口必须使用此格式返回
/// </summary>
/// <remarks>
/// 泛型版本用于携带数据的响应，非泛型版本用于纯状态响应（如 401/409）。
/// Errors 设为 IReadOnlyList 防止中间件/过滤器意外篡改已构造的响应。
/// </remarks>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string Message { get; set; } = string.Empty;
    public IReadOnlyList<string> Errors { get; set; } = Array.Empty<string>();

    public static ApiResponse<T> Ok(T data, string message = "操作成功")
        => new() { Success = true, Data = data, Message = message };

    public static ApiResponse<T> Fail(string message, IReadOnlyList<string>? errors = null)
        => new() { Success = false, Message = message, Errors = errors ?? Array.Empty<string>() };
}

/// <summary>
/// 无数据的 API 响应
/// </summary>
public class ApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public IReadOnlyList<string> Errors { get; set; } = Array.Empty<string>();

    public static ApiResponse Ok(string message = "操作成功")
        => new() { Success = true, Message = message };

    public static ApiResponse Fail(string message, IReadOnlyList<string>? errors = null)
        => new() { Success = false, Message = message, Errors = errors ?? Array.Empty<string>() };
}
