using FluentValidation;
using MediatR;

namespace MusicRec.Shared;

/// <summary>
/// MediatR 管道行为 — 在 Handler 执行前自动调用 FluentValidation 验证器
/// </summary>
/// <remarks>
/// 注册为 IPipelineBehavior 后，MediatR 在调用每个 IRequestHandler 之前
/// 先经过此 Behavior，从而实现声明式验证，无需在每个 Handler 中手写校验逻辑。
/// </remarks>
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);
        var results = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = results
            .SelectMany(r => r.Errors)
            .Select(f => f.ErrorMessage)
            .ToList();

        if (failures.Count != 0)
            throw new ValidationException(failures);

        return await next();
    }
}
