using backend_app.Models;
using backend_app.services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static backend_app.Models.Employee;

namespace backend_app.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmployeesController : ControllerBase
    {
        private readonly EmployeeService _employeeService;

        public EmployeesController(EmployeeService employeeService)
        {
            _employeeService = employeeService;
        }

        [HttpGet]
        [Authorize]
        public IActionResult GetAll()
        {
            var employees = _employeeService.GetAll();
            return Ok(employees);
        }

        [HttpGet("{id}")]
        [Authorize]
        public IActionResult GetById(int id)
        {
            var employee = _employeeService.GetById(id);
            if (employee == null)
                return NotFound(new { message = "Employee not found" });

            return Ok(employee);
        }

        [HttpGet("department/{department}")]
        [Authorize]
        public IActionResult GetByDepartment(string department)
        {
            var employees = _employeeService.GetByDepartment(department);
            if (!employees.Any())
                return NotFound(new { message = $"No employees found in {department}" });

            return Ok(employees);
        }

        [HttpGet("role/{role}")]
        [Authorize]
        public IActionResult GetByRole(string role)
        {
            var employees = _employeeService.GetByRole(role);
            if (!employees.Any())
                return NotFound(new { message = $"No employees found with role {role}" });

            return Ok(employees);
        }

        [HttpGet("status/{status}")]
        [Authorize]
        public IActionResult GetByStatus(string status)
        {
            var employees = _employeeService.GetByStatus(status);
            if (!employees.Any())
                return NotFound(new { message = $"No employees found with status {status}" });

            return Ok(employees);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult Create([FromBody] CreateEmployeeDto dto)
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

            var created = _employeeService.Add(employee);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpDelete("{id}")]
        [Authorize]
        public IActionResult Delete(int id)
        {
            var deleted = _employeeService.Delete(id);
            if (!deleted)
                return NotFound(new { message = "Employee not found" });

            return Ok(new { message = "Employee deleted" });
        }
    }
}