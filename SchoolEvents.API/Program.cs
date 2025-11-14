using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Hangfire;
using Hangfire.SqlServer;
using SchoolEvents.API.Data;
using SchoolEvents.API.Services;
using SchoolEvents.API.Models;
using SchoolEvents.API.Jobs;
using Hangfire.Dashboard;
using Microsoft.Graph;
using Azure.Identity;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// FASE 1: CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:5174")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// FASE 2: Entity Framework
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// FASE 3: Microsoft Graph com Azure Identity
builder.Services.AddSingleton(provider =>
{
    var configuration = provider.GetRequiredService<IConfiguration>();
    
    var clientId = configuration["MicrosoftGraph:ClientId"];
    var clientSecret = configuration["MicrosoftGraph:ClientSecret"];
    var tenantId = configuration["MicrosoftGraph:TenantId"];

    if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret) || string.IsNullOrEmpty(tenantId))
    {
        throw new InvalidOperationException("Microsoft Graph credentials are not configured properly.");
    }

    // Configurar ClientSecretCredential
    var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);

    // Criar GraphServiceClient
    var graphClient = new GraphServiceClient(credential);

    Console.WriteLine("✅ Microsoft Graph Service Client configurado com sucesso");
    return graphClient;
});

// FASE 4: Serviços da Aplicação
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IGraphService, GraphService>();


// FASE 5: Hangfire
var hangfireConnection = builder.Configuration.GetConnectionString("HangfireConnection") ?? connectionString;
Console.WriteLine($"🔍 Hangfire Connection: {hangfireConnection}");

builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(hangfireConnection, new SqlServerStorageOptions
    {
        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
        SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
        QueuePollInterval = TimeSpan.Zero,
        UseRecommendedIsolationLevel = true,
        DisableGlobalLocks = true
    }));

builder.Services.AddHangfireServer();

// FASE 6: JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowReactApp");

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

app.UseHttpsRedirection();

// FASE 7: Inicializar banco de dados
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    
    try
    {
        Console.WriteLine("🔄 Verificando banco de dados...");
        
        var canConnect = await dbContext.Database.CanConnectAsync();
        if (canConnect)
        {
            Console.WriteLine("✅ Conexão com banco estabelecida!");
            
            // Tentar contar usuários (se a tabela existir)
            try 
            {
                var userCount = await dbContext.Users.CountAsync();
                Console.WriteLine($"📊 Total de usuários no banco: {userCount}");
            }
            catch
            {
                Console.WriteLine("ℹ️  Tabela Users não existe ou tem estrutura diferente");
            }
        }
        else
        {
            Console.WriteLine("❌ Não foi possível conectar ao banco");
        }
        
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️  Aviso no banco: {ex.Message}");
        Console.WriteLine("💡 Vamos continuar - o Microsoft Graph pode funcionar sem o banco!");
    }
}

// FASE 8: Hangfire Dashboard
try
{
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        DashboardTitle = "School Events - Microsoft Graph Sync",
        Authorization = new[] { new HangfireAuthorizationFilter() }
    });
    Console.WriteLine("✅ Hangfire Dashboard configurado em /hangfire");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Erro no Hangfire Dashboard: {ex.Message}");
}

// FASE 9: Agendar jobs recorrentes
try
{
    using (var scope = app.Services.CreateScope())
    {
        var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
        
        // Job básico de health check
        recurringJobManager.AddOrUpdate(
            "basic-health-check",
            () => Console.WriteLine("✅ Health check executado: " + DateTime.Now),
            "0 */1 * * *"); // Cron: a cada hora
        
        Console.WriteLine("✅ Job básico agendado: Health Check a cada hora");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Erro ao agendar jobs: {ex.Message}");
    Console.WriteLine("⚠️  Hangfire jobs não funcionarão, mas a API continuará rodando");
}

app.MapControllers();

// Endpoint health check básico
app.MapGet("/health", () => Results.Ok(new { status = "Healthy", timestamp = DateTime.UtcNow }));

Console.WriteLine("🚀 School Events API iniciada com Microsoft Graph Integration!");
Console.WriteLine("📊 Hangfire Dashboard: /hangfire");
Console.WriteLine("📚 Swagger UI: /swagger");
Console.WriteLine("❤️  Health Check: /health");

app.Run();

// Filtro de autorização para o Hangfire Dashboard
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context) => true;
}