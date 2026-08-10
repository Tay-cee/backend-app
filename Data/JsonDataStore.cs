using System.Text.Json;
using backend_app.Models;

namespace backend_app.Data;

public class JsonDataStore
{
    private readonly string _filePath;
    private readonly List<AppUser> _users;
    private readonly object _usersLock = new();
    private readonly SemaphoreSlim _saveLock = new(1, 1);

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

    // Snapshotted under lock so callers iterating this while another request adds a
    // user don't hit a "collection was modified" exception (List<T> isn't thread-safe).
    public IEnumerable<AppUser> Users
    {
        get { lock (_usersLock) return _users.ToList(); }
    }

    public void AddUser(AppUser user)
    {
        lock (_usersLock) _users.Add(user);
    }

    public async Task SaveChangesAsync()
    {
        List<AppUser> snapshot;
        lock (_usersLock) snapshot = _users.ToList();

        await _saveLock.WaitAsync();
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(snapshot, options);
            await File.WriteAllTextAsync(_filePath, json);
        }
        finally
        {
            _saveLock.Release();
        }
    }
}
