namespace backend_app.Models
{
    public class User
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }

        public record RegisterDto(string Username, string Email, string Password);
        public record LoginDto(string Username, string Password);
        public record AuthResponseDto(string token, DateTime Experation);
        public record UserDto(string Username, string Email);

    }

}
