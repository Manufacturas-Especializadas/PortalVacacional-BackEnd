using Application.Features.Employee;
using Application.Features.Employee.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize(Roles = "Department Head,Manager")]
    [Route("api/[controller]")]
    [ApiController]
    public class ApprovalsController : ControllerBase
    {
        private readonly IVacationService _vacationService;

        public ApprovalsController(IVacationService vacationService)
        {
            _vacationService = vacationService;
        }

        [HttpGet]
        [Route("peding")]
        public async Task<IActionResult> GetPedingApprovals()
        {
            var payrollClaim = User.FindFirst("payroll")?.Value;

            if (string.IsNullOrEmpty(payrollClaim))
            {
                return Unauthorized(new { message = "No se encontró el número de nómina en el token actual." });
            }

            if (!int.TryParse(payrollClaim, out int managerPayroll))
            {
                return BadRequest(new { message = "El formato del número de nómina no es válido." });
            }

            var peding = await _vacationService.GetPendingRequestsForManagerAsync(managerPayroll);
            return Ok(peding);
        }

        [HttpPost]
        [Route("process")]
        public async Task<IActionResult> ProcessRequest([FromBody] ProcessApprovalDto dto)
        {
            var payrollClaim = User.FindFirst("payroll")?.Value;

            if (string.IsNullOrEmpty(payrollClaim)) return Unauthorized();

            try
            {
                var result = await _vacationService.ProcessApprovalAsync(int.Parse(payrollClaim), dto);

                return Ok(new
                {
                    message = "Solicitud procesada correctamente"
                });
            }
            catch(Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message,
                });
            }
        } 
    }
}