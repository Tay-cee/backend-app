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

    }
}