using Application.Dtos.Auth;
using Application.Features.Admin.Dtos;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        [HttpPost]
        [Route("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            var result = await _authService.LoginAsync(request);

            return Ok(result);
        }

        [Authorize]
        [HttpPost]
        [Route("setup-profile")]
        public async Task<IActionResult> SetupProfile([FromBody] SetupProfileDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Unauthorized();

            int userId = int.Parse(userIdClaim.Value);

            try
            {
                var success = await _authService.SetupInitialProfileAsync(userId, dto);

                if (!success)
                    return NotFound(new { message = "Usuario no encontrado." });

                return Ok(new { message = "Perfil configurado correctamente. ¡Bienvenido a MESA!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Error al actualizar el perfil: " + ex.Message });
            }
        }
    }
}
