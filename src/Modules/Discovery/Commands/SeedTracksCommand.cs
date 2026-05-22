using MediatR;

namespace MusicRec.Discovery.Commands;

/// <summary>
/// 种子曲目生成命令 — 为探索页面生成模拟曲目数据
/// 自动创建流派、艺术家、专辑及曲目，覆盖 20+ 音乐风格
/// </summary>
public record SeedTracksCommand : IRequest<SeedTracksResult>;

/// <summary>
/// 种子数据生成结果
/// </summary>
public record SeedTracksResult(
    int GenresCreated,
    int ArtistsCreated,
    int AlbumsCreated,
    int TracksCreated);
