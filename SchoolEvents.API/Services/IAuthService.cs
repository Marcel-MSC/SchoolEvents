using SchoolEvents.API.Models;

namespace SchoolEvents.API.Services
{
    public interface IAuthService
    {
        Task<AuthResult?> AuthenticateAsync(LoginModel login);

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
