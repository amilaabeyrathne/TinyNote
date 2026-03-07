using TinyNote.Api.Data.Entities;
using TinyNote.Api.DTOs;
using TinyNote.Api.Repository;

namespace TinyNote.Api.Services;

public class NotesService : INotesService
{
    private readonly INoteRepository _noteRepository;
    private readonly ILogger<NotesService> _logger;

    public NotesService(INoteRepository noteRepository, ILogger<NotesService> logger)
    {
        _noteRepository = noteRepository;
        _logger = logger;
    }

    public async Task<Note> AddNoteAsync(CreateNoteRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Adding note for user {UserId}, title: {Title}", request.UserId, request.Title);

        var summary = request.Content.Length > 50? string.Concat(request.Content.AsSpan(0, 50), "...") : request.Content;

        var note = new Note
        {
            UserId = request.UserId,
            Title = request.Title,
            Content = request.Content,
            Summary = summary,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdateAt = DateTimeOffset.UtcNow
        };

        var addedNote = await _noteRepository.AddNoteAsync(note, cancellationToken);

        _logger.LogInformation("Note added successfully, Id: {NoteId}", addedNote.Id);

        return addedNote;
    }

    public async Task<Note?> GetNoteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _noteRepository.GetNoteAsync(id, cancellationToken);
    }

    public async Task<List<Note>> GetNotesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _noteRepository.GetNotesAsync(userId, cancellationToken);
    }

    public async Task<bool> DeleteNoteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _noteRepository.DeleteNoteAsync(id, cancellationToken);
    }
}
