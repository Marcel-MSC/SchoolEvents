using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using SchoolEvents.API.Data;
using SchoolEvents.API.Models;

namespace SchoolEvents.API.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthService(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<AuthResult?> AuthenticateAsync(LoginModel login)
        {
            if (login.Password == "MICROSOFT_OAUTH")
            {
                return await AuthenticateOrCreateMicrosoftUserAsync(login.Email, "", "");
            }
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == login.Email);

            if (user == null && login.Email == "admin@escola.com" && login.Password == "admin123")
            {
                user = new User
                {
                    Id = Guid.NewGuid().ToString(),
                    Email = login.Email,
                    DisplayName = "Administrador",
                    MicrosoftId = string.Empty,
                    IsActive = true,
                    LastSynced = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }

            if (user == null)
                return null;

            var token = GenerateJwtToken(user);
            
            return new AuthResult
            {
                Success = true,
                Token = token,
                User = user
            };
        }

        public async Task<AuthResult?> AuthenticateOrCreateMicrosoftUserAsync(string email, string displayName, string microsoftId)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

                if (user == null)
                {
                    user = new User
                    {
                        Id = Guid.NewGuid().ToString(),
                        Email = email,
                        DisplayName = string.IsNullOrEmpty(displayName) ? email.Split('@')[0] : displayName,
                        MicrosoftId = microsoftId
                    };
                    
                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();
                    
                    Console.WriteLine($"✅ Novo usuário criado via Microsoft: {email}");
                }
                else
                {
                    if (!string.IsNullOrEmpty(displayName) && user.DisplayName != displayName)
                    {
                        user.DisplayName = displayName;
                        _context.Users.Update(user);
                        await _context.SaveChangesAsync();
                    }
                }

                var token = GenerateJwtToken(user);
                
                return new AuthResult
                {
                    Success = true,
                    Token = token,
                    User = user
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erro ao autenticar/criar usuário Microsoft: {ex.Message}");
                return null;
            }
        }

        private string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]!);
            
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id),
                    new Claim(ClaimTypes.Name, user.DisplayName),
                    new Claim(ClaimTypes.Email, user.Email)
                }),
                Expires = DateTime.UtcNow.AddHours(24),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}