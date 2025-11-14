using SchoolEvents.API.Models;

namespace SchoolEvents.API.Services
{
    public interface IAuthService
    {
        Task<AuthResponse?> AuthenticateAsync(LoginModel login);
        string GenerateJwtToken(User user);
    }
}