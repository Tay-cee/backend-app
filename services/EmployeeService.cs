using backend_app.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using backend_app.DTOs.Employee;

namespace backend_app.services
{
    public class EmployeeService
    {
        private readonly string _filePath;
        private readonly List<Employee> _employees;
        private readonly object _lock = new();

        public EmployeeService(IWebHostEnvironment env)
        {
            _filePath = Path.Combine(env.ContentRootPath, "Data", "employees.json");

            if (!File.Exists(_filePath))
            {
                File.WriteAllText(_filePath, "[]");
                _employees = new();
            }
            else
            {
                var json = File.ReadAllText(_filePath);
                _employees = JsonSerializer.Deserialize<List<Employee>>(json) ?? new();
            }
        }

        public List<Employee> GetAll()
        {
            lock (_lock) return _employees.ToList();
        }

        public Employee? GetById(int id)
        {
            lock (_lock) return _employees.FirstOrDefault(e => e.Id == id);
        }

        public List<Employee> GetByDepartment(string department)
        {
            lock (_lock)
                return _employees.Where(e =>
                    e.Department.Equals(department, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        public List<Employee> GetByRole(string role)
        {
            lock (_lock)
                return _employees.Where(e =>
                    e.Role.Equals(role, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        public List<Employee> GetByStatus(string status)
        {
            lock (_lock)
                return _employees.Where(e =>
                    e.Status.Equals(status, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        public Employee Add([FromBody] CreateEmployeeDto dto)
        {
            lock (_lock)
            {
                var employee = new Employee
                {
                    Username = dto.Username,
                    Email = dto.Email,
                    Role = dto.Role,
                    Department = dto.Department,
                    Status = dto.Status ?? "Active",
                    JoinedAt = DateTime.UtcNow
                };

                employee.Id = _employees.Any() ? _employees.Max(e => e.Id) + 1 : 1;
                _employees.Add(employee);
                SaveChanges();
                return employee;
            }
        }

        public bool Delete(int id)
        {
            lock (_lock)
            {
                var employee = _employees.FirstOrDefault(e => e.Id == id);
                if (employee == null) return false;

                _employees.Remove(employee);
                SaveChanges();
                return true;
            }
        }

        // Caller must hold _lock.
        private void SaveChanges()
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(_employees, options);
            File.WriteAllText(_filePath, json);
        }
    }
}