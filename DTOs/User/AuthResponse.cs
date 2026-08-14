using backend_app.Models;

namespace backend_app.DTOs.User
{
    public record AuthResponse(string AccessToken, string RefreshToken, DateTime AccessTokenExpiresAt, DateTime RefreshTokenExpiresAt, UserDto User);
}
