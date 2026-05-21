using FluentValidation;
using MusicRec.Player.Commands;

namespace MusicRec.Player.Validators;

public class StartPlaybackCommandValidator : AbstractValidator<StartPlaybackCommand>
{
    public StartPlaybackCommandValidator()
    {
        // URI 列表与上下文 URI 是互斥参数，但在 Handler 层做业务校验更合适
        When(x => x.Uris is { Count: > 0 }, () =>
        {
            RuleForEach(x => x.Uris).NotEmpty().WithMessage("曲目 URI 不能为空");
        });
    }
}

public class SetVolumeCommandValidator : AbstractValidator<SetVolumeCommand>
{
    public SetVolumeCommandValidator()
    {
        RuleFor(x => x.VolumePercent)
            .InclusiveBetween(0, 100).WithMessage("音量必须在 0-100 之间");
    }
}

public class SeekToPositionCommandValidator : AbstractValidator<SeekToPositionCommand>
{
    public SeekToPositionCommandValidator()
    {
        RuleFor(x => x.PositionMs)
            .GreaterThanOrEqualTo(0).WithMessage("播放位置不能为负数");
    }
}
