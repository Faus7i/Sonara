using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace MusicRec.Identity.Services;

/// <summary>
/// JWT Token 生成服务
/// </summary>
public class JwtService
{
    private readonly string _secret;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly double _expireHours;

    public JwtService(IConfiguration configuration)
    {
        _secret = configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException("Jwt:Secret 未配置");
        _issuer = configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException("Jwt:Issuer 未配置");
        _audience = configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException("Jwt:Audience 未配置");
        _expireHours = double.TryParse(configuration["Jwt:ExpireHours"], out var hours) ? hours : 24;
    }

    public (string token, DateTime expiresAt) GenerateToken(Guid userId, string email)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var expiresAt = DateTime.UtcNow.AddHours(_expireHours);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
