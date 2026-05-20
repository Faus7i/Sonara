using System.Security.Cryptography;
using MusicRec.Abstractions;

namespace MusicRec.Shared;

/// <summary>
/// PBKDF2 密码哈希实现
/// </summary>
/// <remarks>
/// 使用 RFC 2898 标准的 PBKDF2-SHA256，迭代次数 100,000（OWASP 2023 推荐最低值）。
/// 输出格式："{Base64Salt}.{Base64Hash}"，验证时使用恒定时间比较防止时序攻击。
/// </remarks>
public class PasswordHasher : IPasswordHasher
{
    // OWASP 2023 推荐：PBKDF2-SHA256 最少 100,000 次迭代
    private const int SaltSize = 16;       // 128-bit 盐
    private const int HashSize = 32;       // 256-bit 输出
    private const int Iterations = 100_000;
    private const char Separator = '.';

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
        return $"{Convert.ToBase64String(salt)}{Separator}{Convert.ToBase64String(hash)}";
    }

    public bool Verify(string password, string storedHash)
    {
        var parts = storedHash.Split(Separator);
        if (parts.Length != 2) return false;

        var salt = Convert.FromBase64String(parts[0]);
        var expectedHash = Convert.FromBase64String(parts[1]);
        var actualHash = Rfc2898DeriveBytes.Pbkdf2(
            password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);

        // 恒定时间比较，防止时序攻击推断密码长度
        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }
}
