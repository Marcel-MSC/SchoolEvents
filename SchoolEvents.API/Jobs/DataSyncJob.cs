using Hangfire;
using SchoolEvents.API.Services;
using Microsoft.EntityFrameworkCore;
using SchoolEvents.API.Data;
using SchoolEvents.API.Models;

namespace SchoolEvents.API.Jobs
{
    public class DataSyncJob
    {
        private readonly IGraphService _graphService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DataSyncJob> _logger;

        public DataSyncJob(
            IGraphService graphService,
            ApplicationDbContext context,
            ILogger<DataSyncJob> logger)
        {
            _graphService = graphService;
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Job principal usado pelo Hangfire para sincronizar usuários e eventos do Microsoft Graph
        /// para a base local. Essa assinatura precisa existir por causa do job recorrente "sync-data".
        /// </summary>
        [AutomaticRetry(Attempts = 2)]
        public async Task SyncUsersAndEvents()
        {
            try
            {
                _logger.LogInformation("🔄 Iniciando sincronização AUTOMÁTICA de usuários e eventos do Microsoft Graph...");

                var graphUsers = await _graphService.GetUsersSampleAsync(200);

                var usersSynced = 0;
                var usersUpdated = 0;
                var eventsSynced = 0;

                foreach (var graphUser in graphUsers)
                {

                    if (string.IsNullOrWhiteSpace(graphUser.MicrosoftId))
                    {
                        _logger.LogWarning("⚠️ Usuário ignorado: MicrosoftId vazio. Nome: {DisplayName}, Email: {Email}",
                                    graphUser.DisplayName, graphUser.Email);
                        continue;
                    }
                    // Localizar usuário pela MicrosoftId (id do Graph)
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

                        _context.Users.Update(existingUser);
                        await _context.SaveChangesAsync();

                        userId = existingUser.Id;
                        usersUpdated++;
                    }

                    // Sincronizar eventos desse usuário
                    try
                    {
                        var syncedEventsForUser = await SyncUserEvents(graphUser.MicrosoftId, userId);
                        eventsSynced += syncedEventsForUser;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Erro ao sincronizar eventos para usuário {DisplayName}", graphUser.DisplayName);
                    }
                }

                var totalUsers = await _context.Users.CountAsync();
                var totalEvents = await _context.Events.CountAsync();

                _logger.LogInformation(
                    "✅ Sincronização AUTOMÁTICA concluída: {UsersSynced} novos, {UsersUpdated} atualizados, {EventsSynced} eventos. TotalUsers={TotalUsers}, TotalEvents={TotalEvents}",
                    usersSynced, usersUpdated, eventsSynced, totalUsers, totalEvents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro na sincronização automática de usuários e eventos");
                throw;
            }
        }

        [AutomaticRetry(Attempts = 3)]
        private async Task<int> SyncUserEvents(string userMicrosoftId, string userId)
        {
            try
            {
                // VALIDAÇÃO: Pular se userId for vazio
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("🆔 UserId vazio, pulando sincronização de eventos");
                    return 0;
                }

                var graphEvents = await _graphService.GetUserEventsAsync(userMicrosoftId, 180);
                var eventsSynced = 0;

                foreach (var graphEvent in graphEvents)
                {
                    // VALIDAÇÃO: Pular eventos sem MicrosoftId
                    if (string.IsNullOrEmpty(graphEvent.MicrosoftId))
                    {
                        _logger.LogWarning("📅 Evento sem MicrosoftId para usuário {UserId}, assunto: {Subject}",
                            userId, graphEvent.Subject);
                        continue;
                    }

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
                    else
                    {
                        existingEvent.Subject = graphEvent.Subject;
                        existingEvent.StartTime = graphEvent.StartTime;
                        existingEvent.EndTime = graphEvent.EndTime;
                        existingEvent.Location = graphEvent.Location;
                        existingEvent.IsAllDay = graphEvent.IsAllDay;
                        existingEvent.LastSynced = DateTime.UtcNow;

                        _context.Events.Update(existingEvent);
                    }
                }

                await _context.SaveChangesAsync();

                return eventsSynced;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erro ao sincronizar eventos para usuário {UserMicrosoftId}", userMicrosoftId);
                return 0;
            }
        }

        [AutomaticRetry(Attempts = 2)]
        public async Task CollectMetricsAsync()
        {
            try
            {
                _logger.LogInformation("📊 Coletando métricas do Microsoft Graph...");

                var metrics = await _graphService.GetMetricsAsync();

                if (!string.IsNullOrEmpty(metrics.Error))
                {
                    _logger.LogWarning("⚠️  Métricas com aviso: {Error}", metrics.Error);
                }
                else
                {
                    _logger.LogInformation("📈 Métricas coletadas: {TotalUsers} usuários, {TotalEvents} eventos, {UsersWithEvents} usuários com eventos",
                        metrics.TotalUsers, metrics.TotalEvents, metrics.SampledUsersWithEvents);
                }

                // Aqui você poderia salvar as métricas em uma tabela específica se necessário
                // await SaveMetricsToDatabase(metrics);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao coletar métricas");
                throw;
            }
        }

        [AutomaticRetry(Attempts = 2)]
        public async Task AnalyzeVolumetryAsync()
        {
            try
            {
                _logger.LogInformation("🔍 Analisando volumetria do Microsoft Graph...");

                var result = await _graphService.AnalyzeVolumetryAsync();

                if (!result.Success)
                {
                    _logger.LogWarning("⚠️  Análise de volumetria com problemas: {Error}", result.Error);
                }
                else
                {
                    _logger.LogInformation("📋 Análise de volumetria: {TotalUsers} usuários, {TotalEvents} eventos, {SampledUsers} amostrados, Duração: {Duration}",
                        result.TotalUsers, result.TotalEvents, result.SampledUsers, result.Duration);
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro na análise de volumetria");
                throw;
            }
        }

        [AutomaticRetry(Attempts = 3)]
        public async Task CleanupOldDataAsync()
        {
            try
            {
                _logger.LogInformation("🧹 Iniciando limpeza de dados antigos...");

                var cutoffDate = DateTime.UtcNow.AddMonths(-1); // Manter apenas dados dos últimos 30 dias

                // Limpar eventos antigos
                var oldEvents = await _context.Events
                    .Where(e => e.StartTime < cutoffDate)
                    .ToListAsync();

                if (oldEvents.Any())
                {
                    _context.Events.RemoveRange(oldEvents);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("🗑️  Removidos {Count} eventos antigos", oldEvents.Count);
                }
                else
                {
                    _logger.LogInformation("✅ Nenhum evento antigo para remover");
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro durante a limpeza de dados antigos");
                throw;
            }
        }
    }
}