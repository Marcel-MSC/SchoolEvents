using SchoolEvents.API.Data;
using SchoolEvents.API.Services;
using SchoolEvents.API.Models;
using Microsoft.EntityFrameworkCore;

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

        public async Task SyncUsersAndEvents()
        {
            try
            {
                _logger.LogInformation("🔄 Iniciando sincronização automática...");

                // 1. Buscar usuários do Microsoft Graph
                var graphUsers = await _graphService.GetUsersSampleAsync(100);
                
                var usersSynced = 0;
                var usersUpdated = 0;
                var eventsSynced = 0;

                // 2. Sincronizar cada usuário com o banco
                foreach (var graphUser in graphUsers)
                {
                    var existingUser = await _context.Users
                        .FirstOrDefaultAsync(u => u.MicrosoftId == graphUser.MicrosoftId);

                    string userId;

                    if (existingUser == null)
                    {
                        // Criar novo usuário
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
                        await _context.SaveChangesAsync(); // Salvar para obter o ID
                        userId = user.Id;
                        usersSynced++;
                        _logger.LogDebug("➕ Novo usuário: {DisplayName}", graphUser.DisplayName);
                    }
                    else
                    {
                        // Atualizar usuário existente
                        existingUser.DisplayName = graphUser.DisplayName;
                        existingUser.Email = graphUser.Email;
                        existingUser.JobTitle = graphUser.JobTitle;
                        existingUser.Department = graphUser.Department;
                        existingUser.LastSynced = DateTime.UtcNow;
                        userId = existingUser.Id;
                        usersUpdated++;
                        _logger.LogDebug("📝 Usuário atualizado: {DisplayName}", graphUser.DisplayName);
                    }

                    // 3. Sincronizar eventos deste usuário
                    try
                    {
                        var eventsSyncedForUser = await SyncUserEvents(graphUser.MicrosoftId, userId);
                        eventsSynced += eventsSyncedForUser;
                        
                        if (eventsSyncedForUser > 0)
                        {
                            _logger.LogDebug("📅 {EventCount} eventos sincronizados para {DisplayName}", 
                                eventsSyncedForUser, graphUser.DisplayName);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "⚠️ Erro ao sincronizar eventos para {DisplayName}", graphUser.DisplayName);
                    }
                }

                var totalUsers = await _context.Users.CountAsync();
                var totalEvents = await _context.Events.CountAsync();
                
                _logger.LogInformation("✅ Sync automático concluído: {NewUsers} novos usuários, {UpdatedUsers} atualizados, {Events} eventos, Total: {TotalUsers} usuários, {TotalEvents} eventos", 
                    usersSynced, usersUpdated, eventsSynced, totalUsers, totalEvents);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro na sincronização automática");
            }
        }

        private async Task<int> SyncUserEvents(string userMicrosoftId, string userId)
        {
            try
            {
                // Buscar eventos do usuário no Microsoft Graph
                var graphEvents = await _graphService.GetUserEventsAsync(userMicrosoftId, 50); // Últimos 50 eventos
                
                var eventsSynced = 0;

                foreach (var graphEvent in graphEvents)
                {
                    var existingEvent = await _context.Events
                        .FirstOrDefaultAsync(e => e.MicrosoftId == graphEvent.MicrosoftId && e.UserId == userId);

                    if (existingEvent == null)
                    {
                        // Criar novo evento
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
                        // Atualizar evento existente
                        existingEvent.Subject = graphEvent.Subject;
                        existingEvent.StartTime = graphEvent.StartTime;
                        existingEvent.EndTime = graphEvent.EndTime;
                        existingEvent.Location = graphEvent.Location;
                        existingEvent.IsAllDay = graphEvent.IsAllDay;
                        existingEvent.LastSynced = DateTime.UtcNow;
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
    }
}