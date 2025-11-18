using Moq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SchoolEvents.API.Data;
using SchoolEvents.API.Services;
using SchoolEvents.API.Models;
using Xunit;

namespace SchoolEvents.API.Tests.Services
{
    public class AuthServiceTests : IDisposable
    {
        private readonly AuthService _authService;
        private readonly ApplicationDbContext _context;
        private readonly Mock<IConfiguration> _configurationMock;

        public AuthServiceTests()
        {
            // Configurar DbContext em memória para testes
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase_" + Guid.NewGuid())
                .Options;

            _context = new ApplicationDbContext(options);
            
            // Mock da configuração
            _configurationMock = new Mock<IConfiguration>();
            _configurationMock.Setup(x => x["Jwt:Key"]).Returns("minha-chave-super-secreta-que-tem-que-ter-no-minimo-32-caracteres");
            _configurationMock.Setup(x => x["Jwt:Issuer"]).Returns("TestIssuer");
            _configurationMock.Setup(x => x["Jwt:Audience"]).Returns("TestAudience");

            // Ordem correta dos parâmetros: ApplicationDbContext, IConfiguration
            _authService = new AuthService(_context, _configurationMock.Object);
        }

        public void Dispose()
        {
            _context?.Dispose();
        }

        [Fact]
        public async Task AuthenticateAsync_WithValidAdminCredentials_ReturnsAuthResponse()
        {
            // Arrange
            var loginModel = new LoginModel 
            { 
                Email = "admin@escola.com", 
                Password = "admin123" 
            };

            // Act
            var result = await _authService.AuthenticateAsync(loginModel);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Token);
            Assert.Equal("admin@escola.com", result.User.Email);
            Assert.Equal("Administrador", result.User.DisplayName);
        }

        [Fact]
        public async Task AuthenticateAsync_WithInvalidCredentials_ReturnsNull()
        {
            // Arrange
            var loginModel = new LoginModel 
            { 
                Email = "invalid@email.com", 
                Password = "wrongpassword" 
            };

            // Act
            var result = await _authService.AuthenticateAsync(loginModel);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task AuthenticateAsync_WithValidUserInDatabase_ReturnsAuthResponse()
        {
            // Arrange
            var testUser = new User 
            { 
                Id = "test-user-1", 
                DisplayName = "Test User", 
                Email = "test@escola.com" 
            };
            
            _context.Users.Add(testUser);
            await _context.SaveChangesAsync();

            var loginModel = new LoginModel 
            { 
                Email = "test@escola.com", 
                Password = "anypassword" 
            };

            // Act
            var result = await _authService.AuthenticateAsync(loginModel);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Token);
            Assert.Equal("test@escola.com", result.User.Email);
        }

        [Fact]
        public async Task GenerateJwtToken_WithValidUser_ReturnsValidToken()
        {
            // Em vez de chamar um método privado, usamos o fluxo público de autenticação/criação
            var result = await _authService.AuthenticateOrCreateMicrosoftUserAsync(
                email: "test-token@escola.com",
                displayName: "Test Token User",
                microsoftId: "ms-id-token-1");

            Assert.NotNull(result);
            Assert.False(string.IsNullOrEmpty(result!.Token));
            Assert.Contains(".", result.Token); // JWT tem 3 partes separadas por pontos
        }

        [Fact]
        public async Task AuthenticateAsync_WithNullLoginModel_ReturnsNull()
        {
            // Act
            var result = await _authService.AuthenticateAsync(new LoginModel { Email = "", Password = "" });

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task AuthenticateAsync_WithEmptyEmail_ReturnsNull()
        {
            // Arrange
            var loginModel = new LoginModel 
            { 
                Email = "", 
                Password = "password" 
            };

            // Act
            var result = await _authService.AuthenticateAsync(loginModel);

            // Assert
            Assert.Null(result);
        }
    }
}