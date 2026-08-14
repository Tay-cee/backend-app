namespace backend_app.DTOs.User
{
    public record RefreshRequest(string? AccessToken, string? RefreshToken);
}
