using TinyNote.Api.Data.Entities;
using TinyNote.Api.DTOs;

namespace TinyNote.Api.Services;

public interface INotesService
{
    Task<NoteResponse> AddNoteAsync(CreateNoteRequest request, CancellationToken cancellationToken = default);
    Task<NoteResponse?> GetNoteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<NoteResponse>> GetNotesAsync(GetNotesQuery query, CancellationToken cancellationToken = default);
    Task<bool> DeleteNoteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<NoteResponse> UpdateNoteAsync(UpdateNoteRequest request, CancellationToken cancellationToken = default);

}
