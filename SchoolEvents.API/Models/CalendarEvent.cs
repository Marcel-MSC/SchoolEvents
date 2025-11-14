namespace SchoolEvents.API.Models
{
    public class CalendarEvent
    {
        public string Id { get; set; } = string.Empty;
        public string MicrosoftId { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string? Location { get; set; }
        public bool IsAllDay { get; set; }
        public string UserId { get; set; } = string.Empty;
        public User? User { get; set; }
        public DateTime LastSynced { get; set; } = DateTime.UtcNow;
    }
}