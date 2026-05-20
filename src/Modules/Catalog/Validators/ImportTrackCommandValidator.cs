using FluentValidation;
using MusicRec.Catalog.Commands;

namespace MusicRec.Catalog.Validators;

public class ImportTrackCommandValidator : AbstractValidator<ImportTrackCommand>
{
    public ImportTrackCommandValidator()
    {
        RuleFor(x => x.SpotifyTrackId)
            .NotEmpty().WithMessage("Spotify Track ID 不能为空")
            .MaximumLength(64).WithMessage("Spotify Track ID 长度不能超过 64");
    }
}
