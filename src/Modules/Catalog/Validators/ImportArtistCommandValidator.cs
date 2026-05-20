using FluentValidation;
using MusicRec.Catalog.Commands;

namespace MusicRec.Catalog.Validators;

public class ImportArtistCommandValidator : AbstractValidator<ImportArtistCommand>
{
    public ImportArtistCommandValidator()
    {
        RuleFor(x => x.SpotifyArtistId)
            .NotEmpty().WithMessage("Spotify Artist ID 不能为空")
            .MaximumLength(64).WithMessage("Spotify Artist ID 长度不能超过 64");
    }
}
