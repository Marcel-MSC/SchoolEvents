using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using SchoolEvents.API.Models;
using SchoolEvents.API.Services;
using Microsoft.Graph;
using Microsoft.Identity.Client;

namespace SchoolEvents.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;

        // CORREÇÃO: Parâmetros com nomes diferentes dos campos
        public AuthController(
            IAuthService authServiceParam, 
            IConfiguration configurationParam,
            ILogger<AuthController> loggerParam)
        {
            _authService = authServiceParam;
            _configuration = configurationParam;
            _logger = loggerParam;
        }

        // Configurações do Azure AD
        private string ClientId => _configuration["MicrosoftGraph:ClientId"]!;
        private string ClientSecret => _configuration["MicrosoftGraph:ClientSecret"]!;
        private string TenantId => _configuration["MicrosoftGraph:TenantId"]!;
        private string RedirectUri => _configuration["MicrosoftGraph:RedirectUri"] ?? "http://localhost:3000/auth/callback";

        [HttpGet("microsoft-login-url")]
        [AllowAnonymous]
        public IActionResult GetMicrosoftLoginUrl()
        {
            try
            {
                var scopes = new[] { "Calendars.Read", "User.Read", "offline_access" };
                
                var loginUrl = $"https://login.microsoftonline.com/{TenantId}/oauth2/v2.0/authorize?" +
                    $"client_id={ClientId}" +
                    $"&response_type=code" +
                    $"&redirect_uri={Uri.EscapeDataString(RedirectUri)}" +
                    $"&response_mode=query" +
                    $"&scope={Uri.EscapeDataString(string.Join(" ", scopes))}" +
                    $"&state={Guid.NewGuid()}";

                _logger.LogInformation("🔗 URL de login Microsoft gerada");

                return Ok(new { 
                    success = true,
                    loginUrl,
                    message = "Use esta URL para login com Microsoft"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao gerar URL de login Microsoft");
                return BadRequest(new { 
                    success = false,
                    error = "Erro ao gerar URL de login" 
                });
            }
        }

        [HttpPost("microsoft-callback")]
        [AllowAnonymous]
        public async Task<IActionResult> MicrosoftCallback([FromBody] MicrosoftCallbackRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Code))
                {
                    return BadRequest(new { 
                        success = false,
                        message = "Código de autorização é obrigatório" 
                    });
                }

                // Trocar código por tokens
                var tokenResult = await ExchangeCodeForTokens(request.Code);

                if (!tokenResult.Success)
                {
                    return Unauthorized(new { 
                        success = false,
                        message = tokenResult.Error 
                    });
                }

                // Obter informações do usuário do Microsoft Graph
                var userInfo = await GetMicrosoftUserInfo(tokenResult.AccessToken!);

                // Criar um LoginModel com as informações do Microsoft Graph
                var microsoftLogin = new LoginModel
                {
                    Email = userInfo.Email,
                    // Para Microsoft Auth, não temos senha, então usamos um valor especial
                    Password = "MICROSOFT_OAUTH"
                };

                // Usar o método AuthenticateAsync existente
                var authResult = await _authService.AuthenticateAsync(microsoftLogin);

                if (authResult == null)
                {
                    return Unauthorized(new { 
                        success = false,
                        message = "Usuário não encontrado. Entre em contato com o administrador."
                    });
                }

                _logger.LogInformation("✅ Usuário autenticado via Microsoft: {Email}", userInfo.Email);

                return Ok(new
                {
                    success = true,
                    message = "Autenticação Microsoft realizada com sucesso",
                    token = authResult.Token,
                    user = authResult.User,
                    microsoftAccessToken = tokenResult.AccessToken,
                    refreshToken = tokenResult.RefreshToken,
                    expiresIn = tokenResult.ExpiresIn
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro no callback de autenticação Microsoft");
                return BadRequest(new { 
                    success = false,
                    message = "Erro no processo de autenticação" 
                });
            }
        }

        // MÉTODOS EXISTENTES (mantidos para compatibilidade)
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginModel login)
        {
            var result = await _authService.AuthenticateAsync(login);
            
            if (result == null)
                return Unauthorized(new { 
                    success = false,
                    message = "Email ou senha inválidos" 
                });

            return Ok(new {
                success = true,
                message = "Login realizado com sucesso",
                token = result.Token,
                user = result.User
            });
        }

        [HttpPost("validate")]
        [Authorize]
        public IActionResult ValidateToken()
        {
            // Se chegou aqui, o token JWT é válido
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userName = User.FindFirst(ClaimTypes.Name)?.Value;
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;

            return Ok(new 
            { 
                success = true,
                isValid = true, 
                user = new { userId, userName, userEmail } 
            });
        }

        // MÉTODOS PRIVADOS AUXILIARES
        private async Task<TokenResult> ExchangeCodeForTokens(string code)
        {
            try
            {
                var app = ConfidentialClientApplicationBuilder
                    .Create(ClientId)
                    .WithClientSecret(ClientSecret)
                    .WithAuthority($"https://login.microsoftonline.com/{TenantId}")
                    .WithRedirectUri(RedirectUri)
                    .Build();

                var scopes = new[] { "Calendars.Read", "User.Read", "offline_access" };

                var result = await app.AcquireTokenByAuthorizationCode(scopes, code)
                    .ExecuteAsync();

                return new TokenResult
                {
                    Success = true,
                    AccessToken = result.AccessToken,
                    // MSAL não expõe o refresh token diretamente para confidential clients.
                    // A renovação deve ser feita com AcquireTokenSilent, se for necessário.
                    ExpiresIn = (int)(result.ExpiresOn - DateTimeOffset.Now).TotalSeconds
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao trocar código por token");
                return new TokenResult { Success = false, Error = ex.Message };
            }
        }

        private async Task<MicrosoftUserInfo> GetMicrosoftUserInfo(string accessToken)
        {
            try
            {
                var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                var graphClient = new GraphServiceClient(httpClient);
                var me = await graphClient.Me.GetAsync();

                return new MicrosoftUserInfo
                {
                    Id = me?.Id ?? string.Empty,
                    DisplayName = me?.DisplayName ?? string.Empty,
                    Email = me?.Mail ?? me?.UserPrincipalName ?? string.Empty
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao obter informações do usuário Microsoft");
                throw;
            }
        }
    }

    // MODELS ESPECÍFICOS PARA MICROSOFT AUTH
    public class MicrosoftCallbackRequest
    {
        public string Code { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
    }

    public class RefreshTokenRequest
    {
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class TokenResult
    {
        public bool Success { get; set; }
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public int ExpiresIn { get; set; }
        public string? Error { get; set; }
    }

    public class MicrosoftUserInfo
    {
        public string Id { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}