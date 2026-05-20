using MusicRec.Abstractions;

namespace MusicRec.AudioFeatures.Entities;

/// <summary>
/// 音频特征实体 — 与 Track 一对一关联，存储 Spotify Audio Features 的 10 个维度
/// </summary>
/// <remarks>
/// TrackId 同时为主键和外键，确保与 Track 的一对一关系。
/// 特征值范围为 Spotify 定义的标准范围（如 0.0-1.0）。
/// </remarks>
public class TrackAudioFeature : IEntity
{
    public Guid Id { get; set; }
    public string SpotifyTrackId { get; set; } = string.Empty;

    // ─── 音频特征维度 ──────────────────────────────
    public float Acousticness { get; set; }
    public float Danceability { get; set; }
    public float Energy { get; set; }
    public float Instrumentalness { get; set; }
    public int Key { get; set; }
    public float Liveness { get; set; }
    public float Loudness { get; set; }
    public int Mode { get; set; }
    public float Speechiness { get; set; }
    public float Tempo { get; set; }
    public int TimeSignature { get; set; }
    public float Valence { get; set; }
    public int DurationMs { get; set; }

    /// <summary>
    /// 返回推荐算法使用的 5 维特征向量 [energy, danceability, valence, tempo(normalized), acousticness]
    /// </summary>
    /// <remarks>
    /// Tempo 除以 200 归一化到约 [0,1] 范围 — 典型歌曲 tempo 在 60-200 BPM 之间，
    /// 与 energy/danceability/valence/acousticness（均已为 [0,1]）保持量纲一致，
    /// 以确保余弦相似度计算中各维度权重均衡。
    /// </remarks>
    public float[] ToVector()
    {
        return new[] { Energy, Danceability, Valence, Tempo / 200f, Acousticness };
    }
}
