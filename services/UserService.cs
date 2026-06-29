using backend_app.Models;
using System.Security.Cryptography;
using System.Text.Json;

namespace backend_app.services
{
    public class UserService
    {
        private const string UsersFilePath = "users.json";
        private static readonly object _lock = new();

        public List<User> GetUsers() 
        {
            if (!File.Exists(UsersFilePath)) return new List<User>();

            var json = File.ReadAllText(UsersFilePath);

            return JsonSerializer.Deserialize<List<User>>(json) ?? new List<User>();
        }

        public void SaveUsers(List<User> users)
        {
            var json = JsonSerializer.Serialize(users, new JsonSerializerOptions { WriteIndented = true });
            lock (_lock)
            {
                File.WriteAllText(UsersFilePath, json);
            }
        }

        public User? FindUsername(string username)
        {
            var users = GetUsers();
            return users.FirstOrDefault(u => u.Username == username);
        }

        public void AddUser(User user)
        {
            var users = GetUsers();
            users.Add(user);
            SaveUsers(users);
        }

        public String HashPassword(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(16);
            var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256);

            byte[] hash = pbkdf2.GetBytes(20);

            byte[] hashBytes = new byte[36];
            Array.Copy(salt, 0, hashBytes, 0, 16);
            Array.Copy(hash, 0, hashBytes, 16, 20);

            return Convert.ToBase64String(hashBytes);
        }

        public bool VerifyPassword(string password, string storedHash)
        {
            byte[] hashBytes = Convert.FromBase64String(storedHash);
            byte[] salt = new byte[16];
            Array.Copy(hashBytes, 0, salt, 0, 16);
         
            var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256);
            byte[] hash = pbkdf2.GetBytes(20);
            
            for (int i = 0; i < 20; i++)
            {
                if (hashBytes[i + 16] != hash[i])
                {
                    return false;
                }
            }
            return true;
        }
    }
}
