using System.ComponentModel.DataAnnotations;

namespace backend_app.Models
{
    public class Employee
    {
        public int Id { get; set; }
        public string Username { get; set; } = "";
        public string Email { get; set; } = "";
        public string Role { get; set; } = "";
        public string Department { get; set; } = "";
        public string Status { get; set; } = "Active";
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        public static readonly string[] ValidStatuses = { "Active", "On Leave", "Inactive" };

        public record CreateEmployeeDto(
            [Required, MinLength(2), MaxLength(100)] string Username,
            [Required, EmailAddress, MaxLength(256)] string Email,
            [Required, MaxLength(100)] string Role,
            [Required, MaxLength(100)] string Department,
            [RegularExpression("Active|On Leave|Inactive")] string? Status = "Active"
        );
    }
}