using MediatR;

namespace MusicRec.Recommendation.Commands;

/// <summary>
/// 种子数据生成命令 — 为缺少音频特征的曲目生成模拟 TrackAudioFeature 数据
/// 用于在 Spotify Audio Features API 不可用时支持推荐算法测试
/// </summary>
public record SeedAudioFeaturesCommand : IRequest<SeedAudioFeaturesResult>;

/// <summary>
/// 种子数据生成结果
/// </summary>
public record SeedAudioFeaturesResult(int TracksProcessed, int FeaturesCreated);
