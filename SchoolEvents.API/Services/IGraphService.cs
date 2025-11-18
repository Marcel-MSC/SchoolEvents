using SchoolEvents.API.Models;

namespace SchoolEvents.API.Services
{
    public interface IGraphService
    {
        Task<IEnumerable<User>> GetUsersSampleAsync(int count = 50);
        Task<IEnumerable<CalendarEvent>> GetUserEventsAsync(string userMicrosoftId, int maxResults = 30);
        Task<IEnumerable<CalendarEvent>> GetUserEventsWithTokenAsync(string userAccessToken, string userMicrosoftId, int maxResults = 30);
        
        // Métodos que estavam faltando
        Task<int> GetUsersCountAsync();
        Task<GraphMetrics> GetMetricsAsync();
        Task<VolumetryResult> AnalyzeVolumetryAsync();
    }

    // MOVER AS CLASSES PARA FORA DA INTERFACE
    public class SyncResult
    {
        public int TotalUsers { get; set; }
        public int TotalEvents { get; set; }
        public int SampledUsers { get; set; }
        public TimeSpan Duration { get; set; }
        public string? Error { get; set; }
        public bool Success => Error == null;
    }

    public class GraphMetrics
    {
        public int TotalUsers { get; set; }
        public int TotalEvents { get; set; }
        public int SampledUsers { get; set; }
        public int SampledUsersWithEvents { get; set; }
        public string? Error { get; set; }
        public DateTime CollectedAt { get; set; } = DateTime.UtcNow;
        public bool Success => Error == null;
    }

    public class VolumetryResult
    {
        public bool Success { get; set; }
        public int TotalUsers { get; set; }
        public int TotalEvents { get; set; }
        public int SampledUsers { get; set; }
        public TimeSpan Duration { get; set; }
        public string? Error { get; set; }
    }
}