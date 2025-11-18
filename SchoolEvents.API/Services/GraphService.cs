using Microsoft.Graph;
using Azure.Identity;
using SchoolEvents.API.Models;

namespace SchoolEvents.API.Services
{
    public class GraphService : IGraphService
    {
        private readonly IConfiguration _configuration;
        private readonly string _clientId;
        private readonly string _tenantId;
        private readonly string _clientSecret;

        public GraphService(IConfiguration configuration)
        {
            _configuration = configuration;
            _clientId = _configuration["MicrosoftGraph:ClientId"] ?? throw new ArgumentNullException("MicrosoftGraph:ClientId");
            _tenantId = _configuration["MicrosoftGraph:TenantId"] ?? throw new ArgumentNullException("MicrosoftGraph:TenantId");
            _clientSecret = _configuration["MicrosoftGraph:ClientSecret"] ?? throw new ArgumentNullException("MicrosoftGraph:ClientSecret");
        }

        // MÉTODO PARA OBTER GRAPH CLIENT COM TOKEN DELEGADO
        private GraphServiceClient GetDelegatedGraphClient(string userAccessToken)
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", userAccessToken);

            return new GraphServiceClient(httpClient);
        }

        // MÉTODO PARA OBTER GRAPH CLIENT COM CLIENT CREDENTIALS
        private GraphServiceClient GetApplicationGraphClient()
        {
            var credential = new ClientSecretCredential(_tenantId, _clientId, _clientSecret);
            return new GraphServiceClient(credential);
        }

        // MÉTODO AUXILIAR PARA CONVERTER DateTimeTimeZone PARA DateTime
        private DateTime? ParseDateTimeTimeZone(Microsoft.Graph.Models.DateTimeTimeZone? dateTimeTimeZone)
        {
            if (dateTimeTimeZone?.DateTime == null)
                return null;

            if (DateTime.TryParse(dateTimeTimeZone.DateTime, out DateTime result))
                return result;

            return null;
        }

        public async Task<IEnumerable<User>> GetUsersSampleAsync(int count = 50)
        {
            try
            {
                var graphClient = GetApplicationGraphClient();
                
                var users = await graphClient.Users
                    .GetAsync(requestConfiguration =>
                    {
                        requestConfiguration.QueryParameters.Top = count;
                        requestConfiguration.QueryParameters.Select = new[] { "id", "displayName", "mail", "userPrincipalName" };
                    });

                if (users?.Value == null)
                    return new List<User>();

                // Aqui usamos MicrosoftId para armazenar o ID do usuário no Microsoft Graph.
                return users.Value.Select(u => new User
                {
                    MicrosoftId = u.Id ?? string.Empty,
                    DisplayName = u.DisplayName ?? string.Empty,
                    Email = u.Mail ?? u.UserPrincipalName ?? string.Empty
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erro ao buscar usuários: {ex.Message}");
                throw;
            }
        }

        public async Task<IEnumerable<CalendarEvent>> GetUserEventsAsync(string userMicrosoftId, int maxResults = 180)
        {
            try
            {
                var graphClient = GetApplicationGraphClient();

                // Janela de tempo ampla: do início do ano passado até hoje
                var start = new DateTime(DateTime.UtcNow.Year - 1, 1, 1);
                var end = DateTime.UtcNow;

                var events = await graphClient.Users[userMicrosoftId].CalendarView
                    .GetAsync(requestConfiguration =>
                    {
                        requestConfiguration.QueryParameters.StartDateTime = start.ToString("o");
                        requestConfiguration.QueryParameters.EndDateTime = end.ToString("o");
                        requestConfiguration.QueryParameters.Top = maxResults;
                        requestConfiguration.QueryParameters.Orderby = new[] { "start/dateTime" };
                    });

                if (events?.Value == null)
                    return new List<CalendarEvent>();

                // Mapeia os campos principais do evento do Microsoft Graph para o modelo interno
                return events.Value.Select(e => new CalendarEvent
                {
                    MicrosoftId = e.Id ?? string.Empty,
                    Subject = e.Subject ?? "Sem assunto",
                    StartTime = ParseDateTimeTimeZone(e.Start) ?? DateTime.MinValue,
                    EndTime = ParseDateTimeTimeZone(e.End) ?? DateTime.MinValue,
                    Location = e.Location?.DisplayName,
                    IsAllDay = e.IsAllDay ?? false,
                    UserId = userMicrosoftId
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erro ao buscar eventos para {userMicrosoftId}: {ex.Message}");
                return new List<CalendarEvent>();
            }
        }

        public async Task<IEnumerable<CalendarEvent>> GetUserEventsWithTokenAsync(string userAccessToken, string userMicrosoftId, int maxResults = 30)
        {
            try
            {
                var graphClient = GetDelegatedGraphClient(userAccessToken);

                // Janela de tempo ampla: do início do ano passado até hoje
                var start = new DateTime(DateTime.UtcNow.Year - 1, 1, 1);
                var end = DateTime.UtcNow;

                var events = await graphClient.Me.CalendarView
                    .GetAsync(requestConfiguration =>
                    {
                        requestConfiguration.QueryParameters.StartDateTime = start.ToString("o");
                        requestConfiguration.QueryParameters.EndDateTime = end.ToString("o");
                        requestConfiguration.QueryParameters.Top = maxResults;
                        requestConfiguration.QueryParameters.Orderby = new[] { "start/dateTime" };
                    });

                if (events?.Value == null)
                    return new List<CalendarEvent>();

                return events.Value.Select(e => new CalendarEvent
                {
                    MicrosoftId = e.Id ?? string.Empty,
                    Subject = e.Subject ?? "Sem assunto",
                    StartTime = ParseDateTimeTimeZone(e.Start) ?? DateTime.MinValue,
                    EndTime = ParseDateTimeTimeZone(e.End) ?? DateTime.MinValue,
                    Location = e.Location?.DisplayName,
                    IsAllDay = e.IsAllDay ?? false,
                    UserId = userMicrosoftId
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erro ao buscar eventos com token delegado: {ex.Message}");
                return new List<CalendarEvent>();
            }
        }

        public async Task<int> GetUsersCountAsync()
        {
            try
            {
                var graphClient = GetApplicationGraphClient();
                var users = await graphClient.Users
                    .GetAsync(requestConfiguration =>
                    {
                        requestConfiguration.QueryParameters.Top = 1;
                        requestConfiguration.QueryParameters.Count = true;
                    });

                // CORREÇÃO: Converter long para int com cast explícito
                return users?.OdataCount != null ? (int)users.OdataCount : 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erro ao contar usuários: {ex.Message}");
                return 0;
            }
        }

        public async Task<GraphMetrics> GetMetricsAsync()
        {
            try
            {
                var totalUsers = await GetUsersCountAsync();
                var sampleUsers = await GetUsersSampleAsync(10);
                
                int usersWithEvents = 0;
                int totalEvents = 0;

                foreach (var user in sampleUsers)
                {
                    var events = await GetUserEventsAsync(user.MicrosoftId, 180);
                    if (events.Any())
                    {
                        usersWithEvents++;
                        totalEvents += events.Count();
                    }
                }

                return new GraphMetrics
                {
                    TotalUsers = totalUsers,
                    TotalEvents = totalEvents,
                    SampledUsers = sampleUsers.Count(),
                    SampledUsersWithEvents = usersWithEvents,
                    Error = null
                };
            }
            catch (Exception ex)
            {
                return new GraphMetrics
                {
                    TotalUsers = 0,
                    TotalEvents = 0,
                    SampledUsers = 0,
                    SampledUsersWithEvents = 0,
                    Error = ex.Message
                };
            }
        }

        public async Task<VolumetryResult> AnalyzeVolumetryAsync()
        {
            try
            {
                var startTime = DateTime.Now;
                var metrics = await GetMetricsAsync();
                var duration = DateTime.Now - startTime;

                return new VolumetryResult
                {
                    Success = true,
                    TotalUsers = metrics.TotalUsers,
                    TotalEvents = metrics.TotalEvents,
                    SampledUsers = metrics.SampledUsers,
                    Duration = duration,
                    Error = null
                };
            }
            catch (Exception ex)
            {
                return new VolumetryResult
                {
                    Success = false,
                    TotalUsers = 0,
                    TotalEvents = 0,
                    SampledUsers = 0,
                    Duration = TimeSpan.Zero,
                    Error = ex.Message
                };
            }
        }
    }
}