using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace MusicRec.Infrastructure;

/// <summary>
/// 实体配置程序集注册表 — 各模块的 DI 扩展方法调用 Register 注册自己的 IEntityTypeConfiguration
/// </summary>
/// <remarks>
/// 所有 Register 调用发生在 DI 容器构建阶段（单线程），故无需线程同步。
/// DbContext.OnModelCreating 中调用 ApplyConfigurations 遍历已注册程序集，
/// 实现各模块实体配置与共享 DbContext 的解耦。
/// </remarks>
public static class EntityConfigurationRegistry
{
    private static readonly List<Assembly> _assemblies = new();

    public static void Register(Assembly assembly) => _assemblies.Add(assembly);

    public static void ApplyConfigurations(ModelBuilder modelBuilder)
    {
        foreach (var asm in _assemblies)
            modelBuilder.ApplyConfigurationsFromAssembly(asm);
    }
}
