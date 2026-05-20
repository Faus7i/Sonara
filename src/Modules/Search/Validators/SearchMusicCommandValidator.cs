using FluentValidation;
using MusicRec.Search.Commands;

namespace MusicRec.Search.Validators;

public class SearchMusicCommandValidator : AbstractValidator<SearchMusicCommand>
{
    private static readonly HashSet<string> ValidTypes = new()
    {
        "track", "artist", "album", "playlist", "show", "episode"
    };

    public SearchMusicCommandValidator()
    {
        RuleFor(x => x.Query)
            .NotEmpty().WithMessage("搜索关键词不能为空")
            .MaximumLength(200).WithMessage("搜索关键词不能超过 200 个字符");

        RuleFor(x => x.Limit)
            .InclusiveBetween(1, 50).WithMessage("搜索结果数必须在 1 到 50 之间");
    }
}
