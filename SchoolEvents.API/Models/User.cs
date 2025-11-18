namespace SchoolEvents.API.Models
{
    public class User
    {
        public string Id { get; set; } = string.Empty;
        public string MicrosoftId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? JobTitle { get; set; }
        public string? Department { get; set; }
        public DateTime LastSynced { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        public List<CalendarEvent> Events { get; set; } = new();
    }
}