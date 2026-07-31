using System.Text.Json;
using backend_app.Models;

namespace backend_app.Data;

public class JsonDataStore
{
    private readonly string _filePath;
    private readonly List<AppUser> _users;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public JsonDataStore(IWebHostEnvironment env)
    {
        _filePath = Path.Combine(env.ContentRootPath, "Data", "users.json");

        if (File.Exists(_filePath))
        {
            var json = File.ReadAllText(_filePath);
            _users = JsonSerializer.Deserialize<List<AppUser>>(json) ?? new List<AppUser>();
        }
        else
        {
            _users = new List<AppUser>();
        }
    }

    public IEnumerable<AppUser> Users => _users;
    public void AddUser(AppUser user) => _users.Add(user);

    public async Task SaveChangesAsync()
    {
        await _lock.WaitAsync();
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(_users, options);
            await File.WriteAllTextAsync(_filePath, json);
        }
        finally
        {
            _lock.Release();
        }
    }
}
