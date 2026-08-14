using System.ComponentModel.DataAnnotations;

namespace backend_app.DTOs.Employee
{
    public record CreateEmployeeDto(
        [Required, MinLength(2), MaxLength(100)] string Username,
        [Required, EmailAddress, MaxLength(256)] string Email,
        [Required, MaxLength(100)] string Role,
        [Required, MaxLength(100)] string Department,
        [RegularExpression("Active|On Leave|Inactive")] string? Status = "Active"
    );
}
