using System.ComponentModel.DataAnnotations;

namespace backend_app.DTOs.User
{
    public record LoginRequest(
        [Required, EmailAddress] string Email,
        [Required] string Password);

}
