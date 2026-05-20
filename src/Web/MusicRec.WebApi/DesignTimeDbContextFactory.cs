using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using MusicRec.AudioFeatures;
using MusicRec.Catalog;
using MusicRec.Identity;
using MusicRec.Infrastructure;
using MusicRec.Search;

namespace MusicRec.WebApi;

/// <summary>
/// EF Core 设计时 DbContext 工厂 — 用于 dotnet ef migrations 命令
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<MusicRecDbContext>
{
    public MusicRecDbContext CreateDbContext(string[] args)
    {
        // 注册所有模块的实体配置
        EntityConfigurationRegistry.Register(typeof(MusicRec.Identity.DependencyInjection).Assembly);
        EntityConfigurationRegistry.Register(typeof(Catalog.DependencyInjection).Assembly);
        EntityConfigurationRegistry.Register(typeof(AudioFeatures.DependencyInjection).Assembly);
        EntityConfigurationRegistry.Register(typeof(Search.DependencyInjection).Assembly);

        var optionsBuilder = new DbContextOptionsBuilder<MusicRecDbContext>();
        optionsBuilder.UseSqlServer(
            "Server=.\\SQLEXPRESS;Database=MusicRecDb;Trusted_Connection=True;TrustServerCertificate=True;");
        return new MusicRecDbContext(optionsBuilder.Options);
    }
}
