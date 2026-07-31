using System.Text.Json.Serialization;

namespace backend_app.Models
{
    public class AppUser
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Email { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new() { "User" };
        public List<RefreshToken> RefreshTokens { get; set; } = new();
    }

    public class RefreshToken
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string TokenHash { get; set; } = string.Empty;
        public string? ReplacedByTokenHash { get; set; }
        public string? ParentTokenHash { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? RevokedAt { get; set; }
        public string? ReasonRevoked { get; set; }

        [JsonIgnore]
        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
        [JsonIgnore]
        public bool IsRevoked => RevokedAt is not null;
        [JsonIgnore]
        public bool IsActive => !IsRevoked && !IsExpired;
    }

    public record RegisterRequest(string UserName, string Email, string Password);
    public record LoginRequest(string Email, string Password);
    public record RefreshRequest(string AccessToken, string? RefreshToken);
    public record AuthResponse(string AccessToken, string RefreshToken, DateTime AccessTokenExpiresAt, DateTime RefreshTokenExpiresAt, UserDto User);
    public record UserDto(Guid Id, string UserName, string Email, IEnumerable<string> Roles);
}
