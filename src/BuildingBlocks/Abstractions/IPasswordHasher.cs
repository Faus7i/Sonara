namespace MusicRec.Abstractions;

/// <summary>
/// 密码哈希服务接口 — 统一密码的哈希与验证逻辑，避免各模块重复实现
/// </summary>
public interface IPasswordHasher
{
    /// <summary>对明文密码进行哈希，返回 "盐.哈希" 格式的 Base64 字符串</summary>
    string Hash(string password);

    /// <summary>验证明文密码与已存储的哈希值是否匹配（恒定时间比较，防止时序攻击）</summary>
    bool Verify(string password, string storedHash);
}
