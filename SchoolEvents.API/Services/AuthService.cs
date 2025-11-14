using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SchoolEvents.API.Data;
using SchoolEvents.API.Models;

namespace SchoolEvents.API.Services
{
    public class AuthService : IAuthService
    {
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;

        public AuthService(IConfiguration configuration, ApplicationDbContext context)
        {
            _configuration = configuration;
            _context = context;
        }

        public async Task<AuthResponse?> AuthenticateAsync(LoginModel login)
        {
            // Em produção, isso seria com Identity + Hash de senha
            // Para demo, vamos usar um usuário fixo
            if (login.Email == "admin@escola.com" && login.Password == "admin123")
            {
                var user = new User 
                { 
                    Id = "admin-1",
                    DisplayName = "Administrador",
                    Email = "admin@escola.com"
                };

                var token = GenerateJwtToken(user);
                
                return new AuthResponse
                {
                    Token = token,
                    Expiration = DateTime.UtcNow.AddHours(2),
                    User = user
                };
            }

            // Buscar usuário no banco (se quiser usar usuários reais)
            var dbUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == login.Email);
            if (dbUser != null)
            {
                // Em produção: verificar hash da senha
                // Para demo, qualquer senha funciona para usuários do banco
                var token = GenerateJwtToken(dbUser);
                
                return new AuthResponse
                {
                    Token = token,
                    Expiration = DateTime.UtcNow.AddHours(2),
                    User = dbUser
                };
            }

            return null;
        }

        public string GenerateJwtToken(User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured")));
            
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.DisplayName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}