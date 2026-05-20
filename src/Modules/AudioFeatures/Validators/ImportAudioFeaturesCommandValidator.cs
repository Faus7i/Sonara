using FluentValidation;
using MusicRec.AudioFeatures.Commands;

namespace MusicRec.AudioFeatures.Validators;

public class ImportAudioFeaturesCommandValidator : AbstractValidator<ImportAudioFeaturesCommand>
{
    public ImportAudioFeaturesCommandValidator()
    {
        RuleFor(x => x.SpotifyTrackIds)
            .NotEmpty().WithMessage("Spotify Track ID 列表不能为空");

        RuleFor(x => x.SpotifyTrackIds.Count)
            .LessThanOrEqualTo(100).WithMessage("一次最多导入 100 个曲目的音频特征");
    }
}
