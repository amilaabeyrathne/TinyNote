using TinyNote.Api.Data.Entities;

namespace TinyNote.Api.Repository;

public interface INoteRepository
{
    Task<Note> AddNoteAsync(Note note, CancellationToken cancellationToken = default);
    Task<Note?> UpdateNoteAsync(Guid id, string title, string content, string? summary, CancellationToken cancellationToken = default);
    Task<bool> DeleteNoteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Note?> GetNoteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Note>> GetNotesAsync(Guid userId, CancellationToken cancellationToken = default);
}
