using FluentValidation;
using MusicRec.Discovery.Queries;

namespace MusicRec.Discovery.Validators;

/// <summary>
/// 探索查询校验 — 分页范围 1-100
/// </summary>
public class GetDiscoveryQueryValidator : AbstractValidator<GetDiscoveryQuery>
{
    public GetDiscoveryQueryValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("用户 ID 不能为空");
        RuleFor(x => x.Limit).InclusiveBetween(1, 100).WithMessage("探索数量必须在 1-100 之间");
    }
}

/// <summary>
/// 冷启动查询校验 — 仅校验分页范围
/// </summary>
public class GetColdStartQueryValidator : AbstractValidator<GetColdStartQuery>
{
    public GetColdStartQueryValidator()
    {
        RuleFor(x => x.Limit).InclusiveBetween(1, 100).WithMessage("推荐数量必须在 1-100 之间");
    }
}
