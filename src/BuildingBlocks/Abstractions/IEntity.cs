namespace MusicRec.Abstractions;

/// <summary>
/// 所有实体的基础接口
/// </summary>
public interface IEntity
{
    Guid Id { get; set; }
}
