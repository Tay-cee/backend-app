using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using backend_app.Configuration;
using backend_app.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace backend_app.services;

public interface ITokenService
{
    string GenerateAccessToken(AppUser user);
    (string Token, string Hash) GenerateRefreshToken();
    string HashToken(string token);
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}

public class TokenService : ITokenService
{
    private readonly JwtOptions _jwt;
    private readonly SigningCredentials _creds;

    public TokenService(IOptions<JwtOptions> jwt)
    {
        _jwt = jwt.Value;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Secret));
        _creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    }

    public string GenerateAccessToken(AppUser user)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Name, user.UserName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName),
        };
        claims.AddRange(user.Roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(_jwt.AccessTokenExpirationMinutes),
            signingCredentials: _creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public (string Token, string Hash) GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        var token = Convert.ToBase64String(bytes);
        return (token, HashToken(token));
    }

    public string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }

    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var parameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidAudience = _jwt.Audience,
            ValidateIssuer = true,
            ValidIssuer = _jwt.Issuer,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Secret)),
            ValidateLifetime = false
        };

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, parameters, out var securityToken);
            return securityToken is JwtSecurityToken jwt &&
                   jwt.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.OrdinalIgnoreCase)
                ? principal : null;
        }
        catch { return null; }
    }
}
