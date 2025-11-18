using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using SchoolEvents.API.Data;
using SchoolEvents.API.Services;
using SchoolEvents.API.Models;
using Microsoft.Graph;

namespace SchoolEvents.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IGraphService _graphService;
        private readonly GraphServiceClient _graphClient;
        private readonly ILogger<UsersController> _logger;

        public UsersController(
            ApplicationDbContext context,
            IGraphService graphService,
            ILogger<UsersController> logger,
            GraphServiceClient graphClient)
        {
            _context = context;
            _graphService = graphService;
            _logger = logger;
            _graphClient = graphClient;
        }

        // ENDPOINT PARA O FRONTEND - Usuários paginados
        [HttpGet]
        public async Task<ActionResult<PagedResult<User>>> GetUsers(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] bool onlyWithEvents = false)
        {
            try
            {
                // Validar parâmetros
                page = Math.Max(1, page);
                pageSize = Math.Clamp(pageSize, 1, 100); // Máximo 100 por página

                var query = _context.Users.AsQueryable();
                if (onlyWithEvents)
                {
                    query = query
                        .Where(u => _context.Events.Any(e => e.UserId == u.Id));
                }

                var totalCount = await query.CountAsync();

                var users = await query
                    // .Where(u => u.IsActive)
                    .OrderBy(u => u.DisplayName)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(u => new User
                    {
                        Id = u.Id,
                        MicrosoftId = u.MicrosoftId,
                        DisplayName = u.DisplayName,
                        Email = u.Email,
                        JobTitle = u.JobTitle,
                        Department = u.Department,
                        LastSynced = u.LastSynced
                    })
                    .ToListAsync();

                var result = new PagedResult<User>
                {
                    Items = users,
                    TotalCount = totalCount,
                    CurrentPage = page,
                    PageSize = pageSize
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar usuários paginados");
                // Retornar resultado vazio em caso de erro
                return Ok(new PagedResult<User>
                {
                    Items = new List<User>(),
                    TotalCount = 0,
                    CurrentPage = page,
                    PageSize = pageSize
                });
            }
        }

        // ENDPOINT PARA O FRONTEND - Eventos de um usuário específico
        [HttpGet("{userId}/events")]
        public async Task<ActionResult<List<CalendarEvent>>> GetUserEvents(string userId)
        {
            try
            {
                var events = await _context.Events
                    .Where(e => e.UserId == userId)
                    .OrderBy(e => e.StartTime)
                    .Select(e => new CalendarEvent
                    {
                        Id = e.Id,
                        MicrosoftId = e.MicrosoftId,
                        Subject = e.Subject,
                        StartTime = e.StartTime,
                        EndTime = e.EndTime,
                        Location = e.Location,
                        IsAllDay = e.IsAllDay,
                        UserId = e.UserId
                    })
                    .ToListAsync();

                return Ok(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar eventos do usuário {UserId}", userId);
                return Ok(new List<CalendarEvent>()); // Retorna lista vazia em caso de erro
            }
        }

        // ENDPOINT PARA SINCRONIZAR DADOS DO GRAPH
        [HttpPost("sync")]
        public async Task<ActionResult> SyncUsersFromGraph()
        {
            try
            {
                _logger.LogInformation("🔄 Iniciando sincronização MANUAL do Microsoft Graph...");

                var graphUsers = await _graphService.GetUsersSampleAsync(200);

                var usersSynced = 0;
                var usersUpdated = 0;
                var eventsSynced = 0;

                foreach (var graphUser in graphUsers)
                {
                    var existingUser = await _context.Users
                        .FirstOrDefaultAsync(u => u.MicrosoftId == graphUser.MicrosoftId);

                    string userId;

                    if (existingUser == null)
                    {
                        var user = new User
                        {
                            Id = Guid.NewGuid().ToString(),
                            MicrosoftId = graphUser.MicrosoftId,
                            DisplayName = graphUser.DisplayName,
                            Email = graphUser.Email,
                            JobTitle = graphUser.JobTitle,
                            Department = graphUser.Department,
                            LastSynced = DateTime.UtcNow,
                            IsActive = true
                        };
                        _context.Users.Add(user);
                        await _context.SaveChangesAsync();
                        userId = user.Id;
                        usersSynced++;
                    }
                    else
                    {
                        existingUser.DisplayName = graphUser.DisplayName;
                        existingUser.Email = graphUser.Email;
                        existingUser.JobTitle = graphUser.JobTitle;
                        existingUser.Department = graphUser.Department;
                        existingUser.LastSynced = DateTime.UtcNow;
                        userId = existingUser.Id;
                        usersUpdated++;
                    }

                    // Sincronizar eventos também
                    try
                    {
                        var userEvents = await _graphService.GetUserEventsAsync(graphUser.MicrosoftId, 180);
                        foreach (var graphEvent in userEvents)
                        {
                            var existingEvent = await _context.Events
                                .FirstOrDefaultAsync(e => e.MicrosoftId == graphEvent.MicrosoftId && e.UserId == userId);

                            if (existingEvent == null)
                            {
                                var calendarEvent = new CalendarEvent
                                {
                                    Id = Guid.NewGuid().ToString(),
                                    MicrosoftId = graphEvent.MicrosoftId,
                                    Subject = graphEvent.Subject,
                                    StartTime = graphEvent.StartTime,
                                    EndTime = graphEvent.EndTime,
                                    Location = graphEvent.Location,
                                    IsAllDay = graphEvent.IsAllDay,
                                    UserId = userId,
                                    LastSynced = DateTime.UtcNow
                                };
                                _context.Events.Add(calendarEvent);
                                eventsSynced++;
                            }
                        }
                        await _context.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Erro ao sincronizar eventos para {DisplayName}", graphUser.DisplayName);
                    }
                }

                var totalUsers = await _context.Users.CountAsync();
                var totalEvents = await _context.Events.CountAsync();

                _logger.LogInformation($"✅ Sincronização MANUAL concluída: {usersSynced} novos, {usersUpdated} atualizados, {eventsSynced} eventos");

                return Ok(new
                {
                    message = "Sincronização manual concluída!",
                    usersSynced = usersSynced,
                    usersUpdated = usersUpdated,
                    eventsSynced = eventsSynced,
                    totalUsers = totalUsers,
                    totalEvents = totalEvents
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro na sincronização manual");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("sync-status")]
        public async Task<ActionResult> GetSyncStatus()
        {
            try
            {
                var totalUsers = await _context.Users.CountAsync();
                var lastSync = await _context.Users
                    .OrderByDescending(u => u.LastSynced)
                    .Select(u => u.LastSynced)
                    .FirstOrDefaultAsync();

                // Testar conexão com Graph
                var graphStatus = "Desconhecido";
                try
                {
                    var metrics = await _graphService.GetMetricsAsync();
                    graphStatus = "Conectado";
                }
                catch
                {
                    graphStatus = "Erro na conexão";
                }

                return Ok(new
                {
                    databaseUsers = totalUsers,
                    lastSync = lastSync,
                    microsoftGraphStatus = graphStatus,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar status da sincronização");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // ENDPOINT PARA TESTE DO GRAPH (mantido para debug)
        [HttpGet("test-graph")]
        public async Task<ActionResult> TestGraphConnection()
        {
            try
            {
                var volumetry = await _graphService.AnalyzeVolumetryAsync();
                return Ok(new
                {
                    message = "✅ Conexão com Microsoft Graph funcionando!",
                    volumetry,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao testar conexão com Graph");
                return StatusCode(500, new
                {
                    error = "❌ Falha na conexão com Microsoft Graph",
                    details = ex.Message
                });
            }
        }

        [HttpGet("debug-sync")]
        public async Task<ActionResult> DebugSync()
        {
            try
            {
                _logger.LogInformation("🔍 Iniciando diagnóstico de sincronização...");

                // 1. Verificar status do Graph
                var graphStatus = "Desconhecido";
                int graphUserCount = 0;
                int graphEventCount = 0;

                try
                {
                    var users = await _graphService.GetUsersSampleAsync(5);
                    graphUserCount = users.Count();
                    graphStatus = "Conectado";

                    // Testar eventos para o primeiro usuário
                    if (users.Any())
                    {
                        var firstUser = users.First();
                        var events = await _graphService.GetUserEventsAsync(firstUser.MicrosoftId, 5);
                        graphEventCount = events.Count();
                    }
                }
                catch (Exception ex)
                {
                    graphStatus = $"Erro: {ex.Message}";
                }

                // 2. Verificar banco de dados
                var dbUserCount = await _context.Users.CountAsync();
                var dbEventCount = await _context.Events.CountAsync();
                var lastSync = await _context.Users
                    .OrderByDescending(u => u.LastSynced)
                    .Select(u => u.LastSynced)
                    .FirstOrDefaultAsync();

                return Ok(new
                {
                    microsoftGraph = new
                    {
                        status = graphStatus,
                        usersAvailable = graphUserCount,
                        eventsAvailable = graphEventCount
                    },
                    database = new
                    {
                        users = dbUserCount,
                        events = dbEventCount,
                        lastSync = lastSync
                    },
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no diagnóstico");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("test-permissions/{userMicrosoftId}")]
        public async Task<ActionResult> TestPermissions(string userMicrosoftId)
        {
            try
            {
                _logger.LogInformation("🔐 Testando permissões para usuário {UserMicrosoftId}", userMicrosoftId);

                // Testar diferentes endpoints para ver quais permissões temos
                var tests = new Dictionary<string, Func<Task<object>>>
                {
                    ["Users.Read"] = async () =>
                    {
                        var user = await _graphClient.Users[userMicrosoftId].GetAsync();
                        return new { success = true, user = user?.DisplayName };
                    },
                    ["Calendars.Read"] = async () =>
                    {
                        var events = await _graphClient.Users[userMicrosoftId].Events.GetAsync();
                        return new { success = true, eventCount = events?.Value?.Count ?? 0 };
                    },
                    ["Calendars.ReadBasic"] = async () =>
                    {
                        var calendar = await _graphClient.Users[userMicrosoftId].Calendar.GetAsync();
                        return new { success = true, calendar = calendar?.Name };
                    }
                };

                var results = new Dictionary<string, object>();

                foreach (var test in tests)
                {
                    try
                    {
                        var result = await test.Value();
                        results[test.Key] = new { success = true, data = result };
                    }
                    catch (Exception ex)
                    {
                        results[test.Key] = new { success = false, error = ex.Message };
                    }
                }

                return Ok(new
                {
                    userMicrosoftId = userMicrosoftId,
                    permissionTests = results,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no teste de permissões");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("seed-test-events")]
        public async Task<ActionResult> SeedTestEvents()
        {
            try
            {
                _logger.LogInformation("🎯 Criando eventos de teste...");

                var users = await _context.Users.Take(10).ToListAsync();
                var eventsCreated = 0;
                var random = new Random();

                foreach (var user in users)
                {
                    // Criar 2-5 eventos de teste para cada usuário
                    var eventCount = random.Next(2, 6);

                    for (int i = 0; i < eventCount; i++)
                    {
                        var daysFromNow = random.Next(1, 30);
                        var startTime = DateTime.Today.AddDays(daysFromNow).AddHours(9);
                        var endTime = startTime.AddHours(1 + random.Next(0, 3));

                        var testEvent = new CalendarEvent
                        {
                            Id = Guid.NewGuid().ToString(),
                            MicrosoftId = $"test-event-{user.Id}-{i}",
                            Subject = $"Evento Teste {i + 1} - {user.DisplayName}",
                            StartTime = startTime,
                            EndTime = endTime,
                            Location = i % 2 == 0 ? "Sala de Reuniões" : "Online",
                            IsAllDay = false,
                            UserId = user.Id,
                            LastSynced = DateTime.UtcNow
                        };

                        _context.Events.Add(testEvent);
                        eventsCreated++;
                    }
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ {EventsCreated} eventos de teste criados para {UserCount} usuários",
                    eventsCreated, users.Count);

                return Ok(new
                {
                    message = "Eventos de teste criados com sucesso!",
                    eventsCreated = eventsCreated,
                    usersWithEvents = users.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar eventos de teste");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}