namespace TinyNote.Api.Data.Entities
{
    public class Note : BaseEntity
    {
        public  Guid UserId { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string? Summary { get; set; }
        public DateTimeOffset UpdateAt { get; set; }

        public User User { get; set; }

    }
}
