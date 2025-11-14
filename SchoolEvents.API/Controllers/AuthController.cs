using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using SchoolEvents.API.Models;
using SchoolEvents.API.Services;

namespace SchoolEvents.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel login)
        {
            var result = await _authService.AuthenticateAsync(login);
            
            if (result == null)
                return Unauthorized(new { message = "Email ou senha inválidos" });

            return Ok(result);
        }

        [HttpPost("validate")]
        [Microsoft.AspNetCore.Authorization.Authorize] // Protege este endpoint
        public IActionResult ValidateToken()
        {
            // Se chegou aqui, o token é válido (já passou pelo middleware JWT)
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userName = User.FindFirst(ClaimTypes.Name)?.Value;
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;

            return Ok(new 
            { 
                isValid = true, 
                user = new { userId, userName, userEmail } 
            });
        }
    }
}