using System.ComponentModel.DataAnnotations;

namespace TinyNote.Api.DTOs;

public class CreateNoteRequest
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    public string Title { get; set; }

    [Required]
    public string Content { get; set; }
    public string? Summary { get; set; }
}
