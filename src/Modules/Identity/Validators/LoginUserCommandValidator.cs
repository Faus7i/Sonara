using FluentValidation;
using MusicRec.Identity.Commands;

namespace MusicRec.Identity.Validators;

/// <summary>
/// 登录请求验证器
/// </summary>
public class LoginUserCommandValidator : AbstractValidator<LoginUserCommand>
{
    public LoginUserCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("邮箱不能为空")
            .EmailAddress().WithMessage("邮箱格式不正确");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("密码不能为空");
    }
}
