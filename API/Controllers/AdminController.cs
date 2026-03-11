using Application.Features.Admin;
using Application.Features.Admin.Dtos;
using Application.Features.GetEmployees;
using Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks.Sources;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly IEmployeeImportService _importService;
        private readonly IEmployeeService _employeeService;
        private readonly AppDbContext _context;

        public AdminController(
            IEmployeeImportService importService,
            IEmployeeService employeeService,
            AppDbContext context)
        {
            _importService = importService;
            _employeeService = employeeService;
            _context = context;
        }

        [HttpGet]
        [Route("roles")]
        public async Task<IActionResult> GetRoles()
        {
            var roles = await _context.Roles
                .Select(r => new { r.Id, r.Name })
                .ToListAsync();
            return Ok(roles);
        }

        [HttpGet]
        [Route("managersSelect")]
        public async Task<IActionResult> GetManagersSelect()
        {
            var roles = await _context.Managers
                .Select(r => new { r.Id, r.FullName })
                .ToListAsync();
            return Ok(roles);
        }

        [HttpGet]
        [Route("employees")]
        public async Task<IActionResult> GetEmployees()
        {
            var currentYear = DateTime.UtcNow.Year;

            var employees = await _context.Users
                    .Where(u => u.Role.Name == "Employee")
                     .Select(u => new EmployeeListDto
                     {
                         Id = u.Id,
                         PayRollNumber = u.PayRollNumber,
                         FullName = u.FullName,
                         RoleId = u.RoleId,
                         ManagerId = u.EmployeeProfile!.ManagerId,
                         HireDate = u.EmployeeProfile!.HireDate,
                         Department = u.EmployeeProfile != null && u.EmployeeProfile.Department != null
                        ? u.EmployeeProfile.Department.Name
                        : "Sin departamento",

                         YearsOfService = u.EmployeeProfile != null && u.EmployeeProfile.HireDate != null
                        ? currentYear - u.EmployeeProfile.HireDate.Year
                        : 0,

                         TotalVacationDays = u.VacationBalances
                            .Sum(v => (decimal)(v.AssignedDays - v.UsedDays)),
                         IsActive = u.IsActive
                     })
                    .ToListAsync();

            return Ok(employees);
        }

        [HttpGet]
        [Route("departmentHead-managers")]
        public async Task<IActionResult> GetManagers()
        {
            var currentYear = DateTime.UtcNow.Year;

            var managers = await _context.Managers
                .Include(m => m.Role)
                .Include(m => m.Department)
                .Select(m => new {
                    M = m,
                    U = _context.Users
                        .Include(u => u.EmployeeProfile)
                        .Include(u => u.VacationBalances)
                        .FirstOrDefault(u => u.PayRollNumber == m.PayRollNumber)
                })
                .Select(x => new
                {
                    Id = x.M.Id,
                    PayRollNumber = x.M.PayRollNumber,
                    FullName = x.M.FullName,
                    Email = x.M.Email,
                    RoleId = x.M.RoleId,
                    RoleName = x.M.Role.Name,
                    Department = x.M.Department.Name,
                    DepartmentId = x.M.DepartmentId,
                    YearsOfService = x.U != null && x.U.EmployeeProfile != null
                        ? currentYear - x.U.EmployeeProfile.HireDate.Year
                        : 0,
                    HireDate = x.U != null && x.U.EmployeeProfile != null
                        ? x.U.EmployeeProfile.HireDate
                        : (DateTime?)null,
                    TotalVacationDays = x.U != null
                        ? x.U.VacationBalances.Sum(v => v.AssignedDays - v.UsedDays)
                        : 0,
                    IsActive = x.M.IsActive
                })
                .ToListAsync();

            return Ok(managers);
        }

        [HttpPost]
        [Route("createEmployees")]
        public async Task<IActionResult> CreateEmployee([FromBody] CreateEmployeeDto dto)
        {
            try
            {
                var newId = await _employeeService.CreateEmployeeAsync(dto);

                return CreatedAtAction(nameof(GetEmployees), new { id = newId },
                        new { message = "Empleado registrado con éxito", id = newId });
            }
            catch(Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost]
        [Route("import-employees")]
        public async Task<IActionResult> ImportEmployees(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest("Invalid file");

                using var stream = file.OpenReadStream();

                var result = await _importService.ImportAsync(stream, 2);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.InnerException?.Message ?? ex.Message
                });
            }
        }

        [HttpPatch]
        [Route("employees/{id}")]
        public async Task<IActionResult> UpdateEmployee(int id,[FromBody] UpdateEmployeeDto dto)
        {
            if (id != dto.Id)
            {
                return BadRequest("El ID del empleado no coincide con la petición.");
            }

            try
            {
                var success = await _employeeService.UpdateEmployeeAsync(dto);
                if (!success) return NotFound($"No se encontró el empleado.");

                return Ok(new { message = "Empleado actualizado correctamente" });
            }
            catch (DbUpdateException ex)
            {
                var innerMessage = ex.InnerException?.Message ?? ex.Message;

                return BadRequest(new { message = innerMessage });
            }
        }

        [HttpPatch]
        [Route("reactivateEmployee/{id}")]
        public async Task<IActionResult> ReactivateEmployee(int id)
        {
            try
            {
                var success = await _employeeService.ReactivateEmployeeAsync(id);

                if (!success) return NotFound(new
                { message = "No se encontro el colaborador para reactivar" });

                return Ok(new
                {
                    message = "Colaborador reactivado correctamente"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = "Error al intentar reactivar al empleado: " + ex.Message
                });
            }
        }

        [HttpDelete]
        [Route("deleteEmployees/{id}")]
        public async Task<IActionResult> DeleteEmployee(int id)
        {
            var success = await _employeeService.DeleteEmployeeAsync(id);
            if (!success) return NotFound();

            return Ok(new { message = "Empleado desactivado del sistema" });
        }                        
    }
}