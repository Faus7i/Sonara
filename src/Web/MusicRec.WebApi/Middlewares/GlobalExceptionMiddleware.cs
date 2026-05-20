using System.Text.Json;
using MusicRec.Shared;
using MusicRec.WebApi.Infrastructure;

namespace MusicRec.WebApi.Middlewares;

/// <summary>
/// 全局异常处理中间件 — 统一捕获 DomainException 与未处理异常，输出标准 ApiResponse
/// </summary>
/// <remarks>
/// 必须在管道早期注册（仅晚于 Serilog），确保所有下游中间件和 Controller 的异常都能被捕获。
/// DomainException 使用其内建的 StatusCode；未知异常统一返回 500，不泄露内部细节。
/// </remarks>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
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
        catch (DomainException ex)
        {
            _logger.LogWarning("业务异常: {ErrorCode} - {Message}", ex.ErrorCode, ex.Message);

            context.Response.StatusCode = ex.StatusCode;
            context.Response.ContentType = "application/json; charset=utf-8";

            var response = ApiResponse.Fail(ex.Message);
            if (ex is ValidationException vex)
                response.Errors = vex.ValidationErrors;

            await context.Response.WriteAsync(
                JsonSerializer.Serialize(response, JsonDefaults.CamelCaseOptions));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "未处理的系统异常");

            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json; charset=utf-8";

            await context.Response.WriteAsync(
                JsonSerializer.Serialize(
                    ApiResponse.Fail("服务器内部错误"),
                    JsonDefaults.CamelCaseOptions));
        }
    }
}
