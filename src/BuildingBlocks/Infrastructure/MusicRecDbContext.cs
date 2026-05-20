using Microsoft.EntityFrameworkCore;
using MusicRec.Abstractions;

namespace MusicRec.Infrastructure;

/// <summary>
/// 项目共享 DbContext — 映射到单一数据库 MusicRecDb
/// </summary>
/// <remarks>
/// SaveChangesAsync 自动为新增的 IEntity 分配 Guid（若尚未赋值），
/// 避免依赖数据库的 NEWSEQUENTIALID()，使应用程序完全掌控 ID 生成时机。
/// </remarks>
public class MusicRecDbContext : DbContext
{
    public MusicRecDbContext(DbContextOptions<MusicRecDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // 通过注册表加载各模块的 IEntityTypeConfiguration（解耦模块与 DbContext）
        EntityConfigurationRegistry.ApplyConfigurations(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // 自动为新增实体分配 Guid，确保领域事件发布前 ID 已确定
        foreach (var entry in ChangeTracker.Entries<IEntity>())
        {
            if (entry.State == EntityState.Added && entry.Entity.Id == Guid.Empty)
                entry.Entity.Id = Guid.NewGuid();
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
