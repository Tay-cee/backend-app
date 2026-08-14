using System.ComponentModel.DataAnnotations;

namespace backend_app.DTOs.User
{
    public record RegisterRequest(
        [Required, MinLength(3), MaxLength(50)] string UserName,
        [Required, EmailAddress, MaxLength(256)] string Email,
        [Required, MinLength(8), MaxLength(128)] string Password);
}
