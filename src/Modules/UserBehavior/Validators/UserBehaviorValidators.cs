using FluentValidation;
using MusicRec.UserBehavior.Commands;

namespace MusicRec.UserBehavior.Validators;

public class RecordPlayCommandValidator : AbstractValidator<RecordPlayCommand>
{
    public RecordPlayCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("用户 ID 不能为空");
        RuleFor(x => x.TrackId).NotEmpty().WithMessage("曲目 ID 不能为空");
        RuleFor(x => x.Source)
            .MaximumLength(50).When(x => x.Source is not null)
            .WithMessage("来源标识不能超过 50 个字符");
    }
}

public class FinishPlayCommandValidator : AbstractValidator<FinishPlayCommand>
{
    public FinishPlayCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("用户 ID 不能为空");
        RuleFor(x => x.PlayHistoryId).NotEmpty().WithMessage("播放记录 ID 不能为空");
        RuleFor(x => x.DurationPlayed).GreaterThanOrEqualTo(0).WithMessage("播放时长不能为负数");
    }
}

public class SkipPlayCommandValidator : AbstractValidator<SkipPlayCommand>
{
    public SkipPlayCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("用户 ID 不能为空");
        RuleFor(x => x.PlayHistoryId).NotEmpty().WithMessage("播放记录 ID 不能为空");
        RuleFor(x => x.SkippedAtPositionMs).GreaterThanOrEqualTo(0).WithMessage("跳过位置不能为负数");
    }
}

public class RecordBehaviorEventCommandValidator : AbstractValidator<RecordBehaviorEventCommand>
{
    public RecordBehaviorEventCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("用户 ID 不能为空");
        RuleFor(x => x.EventType).NotEmpty().WithMessage("事件类型不能为空")
            .MaximumLength(50).WithMessage("事件类型不能超过 50 个字符");
        RuleFor(x => x.Context)
            .MaximumLength(100).When(x => x.Context is not null)
            .WithMessage("上下文不能超过 100 个字符");
        RuleFor(x => x.Duration)
            .GreaterThanOrEqualTo(0).When(x => x.Duration.HasValue)
            .WithMessage("停留时长不能为负数");
    }
}

public class RefreshUserProfileCommandValidator : AbstractValidator<RefreshUserProfileCommand>
{
    public RefreshUserProfileCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("用户 ID 不能为空");
    }
}
