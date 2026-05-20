using FluentValidation;
using MusicRec.Identity.Commands;

namespace MusicRec.Identity.Validators;

/// <summary>
/// 更新资料请求验证器
/// </summary>
public class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        RuleFor(x => x.Nickname)
            .NotEmpty().WithMessage("昵称不能为空")
            .MaximumLength(50).WithMessage("昵称不能超过50个字符");

        RuleFor(x => x.AvatarUrl)
            .MaximumLength(500).WithMessage("头像URL不能超过500个字符")
            .When(x => !string.IsNullOrEmpty(x.AvatarUrl));
    }
}
