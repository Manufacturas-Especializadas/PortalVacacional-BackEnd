using Application.Features.Admin;
using Application.Features.Employee.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeeController : ControllerBase
    {
        private readonly IEmployeeService _employeeService;

        public EmployeeController(IEmployeeService employeeService)
        {
            _employeeService = employeeService;
        }

        [HttpGet]
        [Route("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

            var data = await _employeeService.GetDashboardDataAsync(userId);

            return Ok(data);
        }

        [HttpPost]
        [Route("request-vacation")]
        public async Task<IActionResult> RequestVacation([FromBody] CreateVacationRequestDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

            try
            {
                var success = await _employeeService.RequestVacationAsync(userId, dto);

                if (success)
                    return Ok(new { message = "Solicitud enviada con éxito. Tu jefe recibirá una notificación" });

                return BadRequest(new { message = "No se pudo procesar la solicitud" });
            }
            catch(Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}