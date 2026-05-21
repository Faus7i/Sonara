using FluentValidation;
using MusicRec.Recommendation.Commands;
using MusicRec.Recommendation.Queries;

namespace MusicRec.Recommendation.Validators;

/// <summary>
/// 推荐查询校验 — 分页范围 1-100
/// </summary>
public class GetRecommendationsQueryValidator : AbstractValidator<GetRecommendationsQuery>
{
    public GetRecommendationsQueryValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("用户 ID 不能为空");
        RuleFor(x => x.Limit).InclusiveBetween(1, 100).WithMessage("推荐数量必须在 1-100 之间");
    }
}

/// <summary>
/// 相似曲目查询校验 — 分页范围 1-50
/// </summary>
public class GetSimilarTracksQueryValidator : AbstractValidator<GetSimilarTracksQuery>
{
    public GetSimilarTracksQueryValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("用户 ID 不能为空");
        RuleFor(x => x.TrackId).NotEmpty().WithMessage("曲目 ID 不能为空");
        RuleFor(x => x.Limit).InclusiveBetween(1, 50).WithMessage("相似曲目数量必须在 1-50 之间");
    }
}

/// <summary>
/// 种子数据生成校验 — 无参 Command，Validator 满足规则 4 要求
/// </summary>
public class SeedAudioFeaturesCommandValidator : AbstractValidator<SeedAudioFeaturesCommand>
{
}
