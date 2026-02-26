using Application.Features.Admin;
using Application.Features.GetEmployees;
using Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly IEmployeeImportService _importService;
        private readonly AppDbContext _context;

        public AdminController(IEmployeeImportService importService, AppDbContext context)
        {
            _importService = importService;
            _context = context;
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
                         PayRollNumber = u.PayRollNumber,
                         FullName = u.FullName,
                         Department = u.EmployeeProfile != null && u.EmployeeProfile.Department != null
                        ? u.EmployeeProfile.Department.Name
                        : "Sin departamento",

                                 YearsOfService = u.EmployeeProfile != null && u.EmployeeProfile.HireDate != null
                        ? currentYear - u.EmployeeProfile.HireDate.Year
                        : 0,

                         TotalVacationDays = u.VacationBalances
                            .Sum(v => (decimal)(v.AssignedDays - v.UsedDays))
                     })
                    .ToListAsync();

            return Ok(employees);
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
    }
}