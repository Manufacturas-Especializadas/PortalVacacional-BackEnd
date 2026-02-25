using Application.Features.Admin;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly IEmployeeImportService _importService;

        public AdminController(IEmployeeImportService importService)
        {
            _importService = importService;
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