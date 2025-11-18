using SchoolEvents.API.Models;

namespace SchoolEvents.API.Services
{
    public interface IAuthService
    {
        // Autenticação via credenciais (e também usada no fluxo Microsoft com senha especial)
        Task<AuthResult?> AuthenticateAsync(LoginModel login);

        // Autenticação/criação de usuário vindo do Microsoft Graph
        Task<AuthResult?> AuthenticateOrCreateMicrosoftUserAsync(string email, string displayName, string microsoftId);
    }

    public class AuthResult
    {
        public bool Success { get; set; }
        public string? Token { get; set; }
        public User? User { get; set; }
        public string? Message { get; set; }
    }
}
