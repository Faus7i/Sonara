namespace MusicRec.Shared;

/// <summary>
/// 业务领域异常基类 — 携带 HTTP 状态码与错误码，供全局中间件统一处理
/// </summary>
/// <remarks>
/// 所有业务异常必须继承此类，禁止在 Controller/Handler 中 try-catch 后手动构造错误响应。
/// 中间件根据 StatusCode 设置 HTTP 状态码，根据 ErrorCode 输出日志分类。
/// </remarks>
public abstract class DomainException : Exception
{
    public int StatusCode { get; }
    public string ErrorCode { get; }

    protected DomainException(string message, int statusCode = 400, string errorCode = "DOMAIN_ERROR")
        : base(message)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
    }
}

/// <summary>
/// 资源未找到异常 → HTTP 404
/// </summary>
public class NotFoundException : DomainException
{
    public NotFoundException(string message = "资源不存在")
        : base(message, 404, "NOT_FOUND") { }
}

/// <summary>
/// 请求参数验证失败 → HTTP 400，携带具体字段错误列表
/// </summary>
public class ValidationException : DomainException
{
    public IReadOnlyList<string> ValidationErrors { get; }

    public ValidationException(List<string> errors)
        : base("请求参数验证失败", 400, "VALIDATION_ERROR")
    {
        ValidationErrors = errors;
    }

    public ValidationException(string error)
        : this(new List<string> { error }) { }
}

/// <summary>
/// 认证失败 → HTTP 401
/// </summary>
public class UnauthorizedException : DomainException
{
    public UnauthorizedException(string message = "未登录或登录已过期")
        : base(message, 401, "UNAUTHORIZED") { }
}

/// <summary>
/// 业务冲突（如重复注册） → HTTP 409
/// </summary>
public class ConflictException : DomainException
{
    public ConflictException(string message = "资源冲突")
        : base(message, 409, "CONFLICT") { }
}
