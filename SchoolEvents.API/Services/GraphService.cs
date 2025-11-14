using Microsoft.Graph;
using Azure.Identity;
using SchoolEvents.API.Models;

namespace SchoolEvents.API.Services
{
    public class GraphService : IGraphService
    {
        private readonly GraphServiceClient _graphClient;
        private readonly ILogger<GraphService> _logger;

        public GraphService(IConfiguration configuration, ILogger<GraphService> logger)
        {
            _logger = logger;

            var clientId = configuration["MicrosoftGraph:ClientId"];
            var clientSecret = configuration["MicrosoftGraph:ClientSecret"];
            var tenantId = configuration["MicrosoftGraph:TenantId"];

            var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
            _graphClient = new GraphServiceClient(credential);
        }

        public async Task<int> GetUsersCountAsync()
        {
            try
            {
                var usersResponse = await _graphClient.Users
                    .GetAsync(requestConfiguration =>
                    {
                        requestConfiguration.QueryParameters.Select = new[] { "id" };
                        requestConfiguration.QueryParameters.Top = 1;
                    });

                return usersResponse?.Value?.Count ?? 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao contar usuários");
                throw;
            }
        }

        public async Task<IEnumerable<User>> GetUsersSampleAsync(int maxUsers = 50)
        {
            try
            {
                var usersResponse = await _graphClient.Users
                    .GetAsync(requestConfiguration =>
                    {
                        requestConfiguration.QueryParameters.Select = new[] { "id", "displayName", "mail", "userPrincipalName", "jobTitle", "department" };
                        requestConfiguration.QueryParameters.Top = maxUsers;
                    });

                if (usersResponse?.Value == null)
                    return Enumerable.Empty<User>();

                return usersResponse.Value.Select(u => new User
                {
                    MicrosoftId = u.Id ?? string.Empty,
                    DisplayName = u.DisplayName ?? "Sem nome",
                    Email = u.Mail ?? u.UserPrincipalName ?? string.Empty,
                    JobTitle = u.JobTitle,
                    Department = u.Department
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter amostra de usuários");
                throw;
            }
        }

        public async Task<IEnumerable<CalendarEvent>> GetUserEventsAsync(string userId, int maxEvents = 50)
        {
            try
            {
                _logger.LogInformation("📅 Buscando eventos para usuário {UserId}", userId);

                // Abordagem mais simples: buscar eventos sem filtro de data
                var eventsResponse = await _graphClient.Users[userId].Events
                    .GetAsync(requestConfiguration =>
                    {
                        requestConfiguration.QueryParameters.Select = new[] { "id", "subject", "start", "end", "location", "isAllDay" };
                        requestConfiguration.QueryParameters.Top = maxEvents;
                        requestConfiguration.QueryParameters.Orderby = new[] { "start/dateTime desc" }; // Mais recentes primeiro
                    });

                if (eventsResponse?.Value == null)
                {
                    _logger.LogWarning("❌ Nenhum evento encontrado para usuário {UserId}", userId);
                    return Enumerable.Empty<CalendarEvent>();
                }

                var result = new List<CalendarEvent>();

                foreach (var e in eventsResponse.Value)
                {
                    if (e.Start != null && e.Start.DateTime != null && e.End != null && e.End.DateTime != null)
                    {
                        result.Add(new CalendarEvent
                        {
                            MicrosoftId = e.Id ?? string.Empty,
                            Subject = e.Subject ?? "Sem assunto",
                            StartTime = DateTime.Parse(e.Start.DateTime),
                            EndTime = DateTime.Parse(e.End.DateTime),
                            Location = e.Location?.DisplayName,
                            IsAllDay = e.IsAllDay ?? false
                        });
                    }
                    else
                    {
                        _logger.LogDebug("⚠️ Evento com dados incompletos ignorado: {Subject}", e.Subject);
                    }
                }

                _logger.LogInformation("✅ Encontrados {Count} eventos para usuário {UserId}", result.Count, userId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao buscar eventos do usuário {UserId}", userId);
                return Enumerable.Empty<CalendarEvent>();
            }
        }

        public async Task<SyncResult> AnalyzeVolumetryAsync()
        {
            var result = new SyncResult();
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                _logger.LogInformation("📊 Analisando volumetria do Microsoft Graph...");

                // 1. Contar usuários
                result.TotalUsers = await GetUsersCountAsync();

                // 2. Amostra de usuários
                var usersSample = await GetUsersSampleAsync(20);
                result.SampledUsers = usersSample.Count();

                _logger.LogInformation("👥 Encontrados {Count} usuários na amostra", result.SampledUsers);

                // 3. Amostra de eventos (apenas 3 usuários para teste)
                int usersWithEvents = 0;
                foreach (var user in usersSample.Take(3))
                {
                    try
                    {
                        _logger.LogInformation("📅 Buscando eventos para usuário: {UserName}", user.DisplayName);
                        var events = await GetUserEventsAsync(user.MicrosoftId, 5);
                        result.TotalEvents += events.Count();
                        usersWithEvents++;

                        _logger.LogInformation("✅ Usuário {UserName}: {EventCount} eventos", user.DisplayName, events.Count());
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "⚠️  Não foi possível buscar eventos para usuário {UserId}", user.MicrosoftId);
                    }
                }

                _logger.LogInformation("✅ Volumetria analisada: {Users} usuários totais, {Sampled} na amostra, {Events} eventos em {UsersWithEvents} usuários",
                    result.TotalUsers, result.SampledUsers, result.TotalEvents, usersWithEvents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro na análise de volumetria");
                result.Error = ex.Message;
            }
            finally
            {
                stopwatch.Stop();
                result.Duration = stopwatch.Elapsed;
            }

            return result;
        }

        public async Task<GraphMetrics> GetMetricsAsync()
        {
            var metrics = new GraphMetrics();

            try
            {
                _logger.LogInformation("📊 Coletando métricas do Microsoft Graph...");

                // 1. Contar usuários
                var usersResponse = await _graphClient.Users
                    .GetAsync(requestConfiguration =>
                    {
                        requestConfiguration.QueryParameters.Select = new[] { "id" };
                        requestConfiguration.QueryParameters.Top = 5;
                    });

                metrics.TotalUsers = usersResponse?.Value?.Count ?? 0;
                metrics.SampledUsers = metrics.TotalUsers;

                // 2. Para cada usuário, tentar contar eventos (amostra pequena)
                if (usersResponse?.Value != null)
                {
                    foreach (var user in usersResponse.Value.Take(3))
                    {
                        try
                        {
                            var eventsResponse = await _graphClient.Users[user.Id].Events
                                .GetAsync(requestConfiguration =>
                                {
                                    requestConfiguration.QueryParameters.Select = new[] { "id" };
                                    requestConfiguration.QueryParameters.Top = 5;
                                });

                            metrics.TotalEvents += eventsResponse?.Value?.Count ?? 0;
                            metrics.SampledUsersWithEvents++;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Não foi possível acessar eventos do usuário {UserId}", user.Id);
                        }
                    }
                }

                _logger.LogInformation("✅ Métricas coletadas: {Users} usuários, ~{Events} eventos",
                    metrics.TotalUsers, metrics.TotalEvents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao coletar métricas do Microsoft Graph");
                metrics.Error = ex.Message;
            }

            return metrics;
        }

        #region Métodos Privados para Busca de Eventos

        private async Task<IEnumerable<CalendarEvent>> TryCalendarViewWithUTC(string userId, int maxEvents)
        {
            var startTime = DateTimeOffset.UtcNow;
            var endTime = startTime.AddDays(30);

            var eventsResponse = await _graphClient.Users[userId].CalendarView
                .GetAsync(requestConfiguration =>
                {
                    requestConfiguration.Headers.Add("Prefer", "outlook.timezone=\"UTC\"");
                    requestConfiguration.QueryParameters.Select = new[] { "id", "subject", "start", "end", "location", "isAllDay" };
                    requestConfiguration.QueryParameters.Top = maxEvents;
                    requestConfiguration.QueryParameters.StartDateTime = startTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
                    requestConfiguration.QueryParameters.EndDateTime = endTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
                });

            return ProcessEvents(eventsResponse?.Value);
        }

        private async Task<IEnumerable<CalendarEvent>> TryCalendarViewWithTimeZone(string userId, int maxEvents)
        {
            var startTime = DateTimeOffset.Now;
            var endTime = startTime.AddDays(30);

            var eventsResponse = await _graphClient.Users[userId].CalendarView
                .GetAsync(requestConfiguration =>
                {
                    requestConfiguration.Headers.Add("Prefer", "outlook.timezone=\"America/Sao_Paulo\"");
                    requestConfiguration.QueryParameters.Select = new[] { "id", "subject", "start", "end", "location", "isAllDay" };
                    requestConfiguration.QueryParameters.Top = maxEvents;
                    requestConfiguration.QueryParameters.StartDateTime = startTime.ToString("yyyy-MM-ddTHH:mm:ss.fff");
                    requestConfiguration.QueryParameters.EndDateTime = endTime.ToString("yyyy-MM-ddTHH:mm:ss.fff");
                });

            return ProcessEvents(eventsResponse?.Value);
        }

        private async Task<IEnumerable<CalendarEvent>> TryEventsEndpointWithFilter(string userId, int maxEvents)
        {
            var eventsResponse = await _graphClient.Users[userId].Events
                .GetAsync(requestConfiguration =>
                {
                    requestConfiguration.QueryParameters.Select = new[] { "id", "subject", "start", "end", "location", "isAllDay" };
                    requestConfiguration.QueryParameters.Top = maxEvents;
                    // ✅ CORREÇÃO: Formato correto para o filtro - usar aspas simples
                    requestConfiguration.QueryParameters.Filter = "start/dateTime ge '" + DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") + "'";
                    requestConfiguration.QueryParameters.Orderby = new[] { "start/dateTime" };
                });

            return ProcessEvents(eventsResponse?.Value);
        }

        private async Task<IEnumerable<CalendarEvent>> TryEventsEndpointWithoutFilter(string userId, int maxEvents)
        {
            var eventsResponse = await _graphClient.Users[userId].Events
                .GetAsync(requestConfiguration =>
                {
                    requestConfiguration.QueryParameters.Select = new[] { "id", "subject", "start", "end", "location", "isAllDay" };
                    requestConfiguration.QueryParameters.Top = maxEvents;
                    requestConfiguration.QueryParameters.Orderby = new[] { "start/dateTime desc" };
                });

            return ProcessEvents(eventsResponse?.Value);
        }

        private IEnumerable<CalendarEvent> ProcessEvents(IList<Microsoft.Graph.Models.Event>? events)
        {
            if (events == null) return Enumerable.Empty<CalendarEvent>();

            var result = new List<CalendarEvent>();

            foreach (var e in events)
            {
                if (e.Start != null && e.Start.DateTime != null && e.End != null && e.End.DateTime != null)
                {
                    result.Add(new CalendarEvent
                    {
                        MicrosoftId = e.Id ?? string.Empty,
                        Subject = e.Subject ?? "Sem assunto",
                        StartTime = DateTime.Parse(e.Start.DateTime),
                        EndTime = DateTime.Parse(e.End.DateTime),
                        Location = e.Location?.DisplayName,
                        IsAllDay = e.IsAllDay ?? false
                    });
                }
            }

            return result;
        }

        #endregion
    }
}