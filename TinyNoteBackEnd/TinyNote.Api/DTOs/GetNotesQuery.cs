using System.ComponentModel.DataAnnotations;

namespace TinyNote.Api.DTOs;

public class GetNotesQuery
{
    [Required(ErrorMessage = "UserId is required.")]
    public Guid UserId { get; set; }

    public string? Search { get; set; }

    [RegularExpression("^(title|createdAt)$", ErrorMessage = "SortBy must be 'title' or 'createdAt'.")]
    public string SortBy { get; set; } = "createdAt";

    [RegularExpression("^(asc|desc)$", ErrorMessage = "SortOrder must be 'asc' or 'desc'.")]
    public string SortOrder { get; set; } = "desc";
}
