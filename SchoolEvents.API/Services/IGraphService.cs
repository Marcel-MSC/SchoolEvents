using SchoolEvents.API.Models;

namespace SchoolEvents.API.Services
{
    public interface IGraphService
    {
        // Métodos básicos para análise de volumetria
        Task<int> GetUsersCountAsync();
        Task<IEnumerable<User>> GetUsersSampleAsync(int maxUsers = 50);
        Task<IEnumerable<CalendarEvent>> GetUserEventsAsync(string userId, int maxEvents = 50);
        Task<SyncResult> AnalyzeVolumetryAsync();
        Task<GraphMetrics> GetMetricsAsync();

    }

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
    }
}