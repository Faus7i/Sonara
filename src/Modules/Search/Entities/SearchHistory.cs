using MusicRec.Abstractions;

namespace MusicRec.Search.Entities;

/// <summary>
/// 用户搜索历史记录
/// </summary>
public class SearchHistory : IEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Keyword { get; set; } = string.Empty;
    public DateTime SearchedAt { get; set; } = DateTime.UtcNow;
}
