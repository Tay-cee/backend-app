namespace backend_app.DTOs.User
{
    public record UserDto(Guid Id, string UserName, string Email, IEnumerable<string> Roles);
}
