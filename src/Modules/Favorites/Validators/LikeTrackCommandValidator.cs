using FluentValidation;
using MusicRec.Favorites.Commands;

namespace MusicRec.Favorites.Validators;

public class LikeTrackCommandValidator : AbstractValidator<LikeTrackCommand>
{
    public LikeTrackCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("用户 ID 不能为空");
        RuleFor(x => x.TrackId).NotEmpty().WithMessage("曲目 ID 不能为空");
    }
}
