using TinyNote.Api.Data.Entities;

namespace TinyNote.Api.Repository;

public interface INoteRepository
{
    Task<Note> AddNoteAsync(Note note, CancellationToken cancellationToken = default);
    Task<Note?> UpdateNoteAsync(Note note, CancellationToken cancellationToken = default);
    Task<bool> DeleteNoteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Note?> GetNoteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Note>> GetNotesAsync(Guid userId, string? search, string sortBy, string sortOrder, CancellationToken cancellationToken = default);
}
