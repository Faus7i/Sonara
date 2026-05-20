using FluentValidation;
using MusicRec.Identity.Commands;

namespace MusicRec.Identity.Validators;

/// <summary>
/// 注册请求验证器
/// </summary>
public class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("邮箱不能为空")
            .EmailAddress().WithMessage("邮箱格式不正确");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("密码不能为空")
            .MinimumLength(6).WithMessage("密码长度不能少于6位");

        RuleFor(x => x.Nickname)
            .NotEmpty().WithMessage("昵称不能为空")
            .MaximumLength(50).WithMessage("昵称不能超过50个字符");
    }
}
