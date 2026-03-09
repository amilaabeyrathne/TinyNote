using System.ComponentModel.DataAnnotations;

namespace TinyNote.Api.DTOs
{
    public class UpdateNoteRequest
    {
        [Required]
        public Guid UserId { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The Title cannot exceed 100 characters. ")]
        public string Title { get; set; }

        [Required]
        [StringLength(2000, ErrorMessage = "The Content cannot exceed 2000 characters. ")]
        public string Content { get; set; }
    }
}
