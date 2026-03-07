namespace TinyNote.Api.DTOs
{
    public class CreateOrUpdateNoteResponse
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Title { get; set; } 
        public string Content { get; set; } 
        public string? Summary { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdateAt { get; set; }
    }
}
