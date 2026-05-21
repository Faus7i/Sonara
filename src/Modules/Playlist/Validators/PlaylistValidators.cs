using FluentValidation;
using MusicRec.Playlists.Commands;

namespace MusicRec.Playlists.Validators;

public class CreatePlaylistCommandValidator : AbstractValidator<CreatePlaylistCommand>
{
    public CreatePlaylistCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("歌单名称不能为空")
            .MaximumLength(200).WithMessage("歌单名称不能超过 200 个字符");
        RuleFor(x => x.UserId).NotEmpty().WithMessage("用户 ID 不能为空");
    }
}

public class UpdatePlaylistCommandValidator : AbstractValidator<UpdatePlaylistCommand>
{
    public UpdatePlaylistCommandValidator()
    {
        RuleFor(x => x.PlaylistId).NotEmpty().WithMessage("歌单 ID 不能为空");
        RuleFor(x => x.UserId).NotEmpty().WithMessage("用户 ID 不能为空");

        // 仅当客户端明确传值时校验，null 表示"不更新该字段"
        When(x => x.Name is not null, () =>
        {
            RuleFor(x => x.Name).MaximumLength(200).WithMessage("歌单名称不能超过 200 个字符");
        });
        When(x => x.Description is not null, () =>
        {
            RuleFor(x => x.Description).MaximumLength(1000).WithMessage("歌单描述不能超过 1000 个字符");
        });
        When(x => x.CoverImageUrl is not null, () =>
        {
            RuleFor(x => x.CoverImageUrl).MaximumLength(500).WithMessage("封面 URL 不能超过 500 个字符");
        });
    }
}

public class AddTrackToPlaylistCommandValidator : AbstractValidator<AddTrackToPlaylistCommand>
{
    public AddTrackToPlaylistCommandValidator()
    {
        RuleFor(x => x.PlaylistId).NotEmpty().WithMessage("歌单 ID 不能为空");
        RuleFor(x => x.UserId).NotEmpty().WithMessage("用户 ID 不能为空");
        RuleFor(x => x.TrackId).NotEmpty().WithMessage("曲目 ID 不能为空");
    }
}

public class ReorderPlaylistTracksCommandValidator : AbstractValidator<ReorderPlaylistTracksCommand>
{
    public ReorderPlaylistTracksCommandValidator()
    {
        RuleFor(x => x.PlaylistId).NotEmpty().WithMessage("歌单 ID 不能为空");
        RuleFor(x => x.UserId).NotEmpty().WithMessage("用户 ID 不能为空");
        RuleFor(x => x.TrackIds).NotEmpty().WithMessage("曲目顺序列表不能为空");
    }
}
