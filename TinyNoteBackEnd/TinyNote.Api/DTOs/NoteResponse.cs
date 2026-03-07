namespace TinyNote.Api.DTOs
{
    public class NoteResponse
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string? Summary { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
