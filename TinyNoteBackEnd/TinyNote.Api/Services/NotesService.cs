using AutoMapper;
using TinyNote.Api.Data.Entities;
using TinyNote.Api.DTOs;
using TinyNote.Api.Exceptions;
using TinyNote.Api.Metrics;
using TinyNote.Api.Repository;

namespace TinyNote.Api.Services;

public class NotesService : INotesService
{
    private readonly INoteRepository _noteRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<NotesService> _logger;
    private readonly TinyNoteMetrics _metrics;

    public NotesService(INoteRepository noteRepository, IMapper mapper, ILogger<NotesService> logger, TinyNoteMetrics metrics)
    {
        _noteRepository = noteRepository;
        _logger = logger;
        _mapper = mapper;
        _metrics = metrics;
    }

    public async Task<NoteResponse> AddNoteAsync(CreateNoteRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Adding note for user {UserId}, title: {Title}", request.UserId, request.Title);

        var note = new Note
        {
            UserId = request.UserId,
            Title = request.Title,
            Content = request.Content,
            Summary = GetSummary(request.Content),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdateAt = DateTimeOffset.UtcNow
        };

        var addedNote = await _noteRepository.AddNoteAsync(note, cancellationToken);

        _logger.LogInformation("Note added successfully, Id: {NoteId}", addedNote.Id);

        _metrics.NotesCreated.Add(1, new KeyValuePair<string, object?>("user.id", request.UserId.ToString()));

        return _mapper.Map<NoteResponse>(addedNote);
    }

    public async Task<NoteResponse> UpdateNoteAsync(UpdateNoteRequest request, CancellationToken cancellationToken = default)
    {
        var existingNote = await _noteRepository.GetNoteAsync(request.Id, cancellationToken);
        if (existingNote == null)
        {
            _logger.LogWarning("Failed to update note with Id {NoteId}. Note not found.", request.Id);
            throw new ItemNotFoundException(request.Id);
        }
        existingNote.Title = request.Title;
        existingNote.Content = request.Content;
        existingNote.Summary = GetSummary(request.Content);
        existingNote.UpdateAt = DateTimeOffset.UtcNow;

        var updatedNote = await _noteRepository.UpdateNoteAsync(existingNote, cancellationToken);
        _logger.LogInformation("Note with Id {NoteId} updated successfully", request.Id);

        _metrics.NotesUpdated.Add(1);

        return _mapper.Map<NoteResponse>(updatedNote);
    }

    public async Task<NoteResponse?> GetNoteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _noteRepository.GetNoteAsync(id, cancellationToken);
        return result is null ? null : _mapper.Map<NoteResponse>(result);
    }

    public async Task<List<NoteResponse>> GetNotesAsync(GetNotesQuery query, CancellationToken cancellationToken = default)
    {
        var result = await _noteRepository.GetNotesAsync(
            query.UserId,
            query.Search,
            query.SortBy,
            query.SortOrder,
            cancellationToken);
        return _mapper.Map<List<NoteResponse>>(result);
    }

    public async Task<bool> DeleteNoteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (await _noteRepository.DeleteNoteAsync(id, cancellationToken))
        {
            _logger.LogInformation("Note with Id {NoteId} deleted successfully", id);
            _metrics.NotesDeleted.Add(1);
            return true;
        }
        _logger.LogWarning("Failed to delete note with Id {NoteId}. Note not found.", id);
        return false;
    }
    private static string GetSummary(string content)
    {
        return content.Length > 50 ? string.Concat(content.AsSpan(0, 50), "...") : content;
    }
}
