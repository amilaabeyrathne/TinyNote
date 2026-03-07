namespace TinyNote.Api.Data.Entities
{
    public class User : BaseEntity
    {
        public string UserName { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }

        public ICollection<Note> Notes { get; set; } = new List<Note>();
    }
}
