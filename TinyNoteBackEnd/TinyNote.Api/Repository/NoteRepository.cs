using Microsoft.EntityFrameworkCore;
using TinyNote.Api.Data;
using TinyNote.Api.Data.Entities;

namespace TinyNote.Api.Repository;

public class NoteRepository : INoteRepository
{
    private readonly NotesDbContext _context;

    public NoteRepository(NotesDbContext context)
    {
        _context = context;
    }

    public async Task<Note> AddNoteAsync(Note note, CancellationToken cancellationToken = default)
    {
        note.Id = Guid.NewGuid();

        await _context.Notes.AddAsync(note, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return note;
    }

    public async Task<Note?> UpdateNoteAsync(Note note, CancellationToken cancellationToken = default)
    {
        var noteToUpdate = await _context.Notes.FindAsync([note.Id], cancellationToken);
        if (note is null)
            return null;

        note.Title = note.Title;
        note.Content = note.Content;
        note.Summary = note.Summary;
        note.UpdateAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return note;
    }

    public async Task<bool> DeleteNoteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var note = await _context.Notes.FindAsync([id], cancellationToken);
        if (note is null)
            return false;

        _context.Notes.Remove(note);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<Note?> GetNoteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Notes.FirstOrDefaultAsync(n => n.Id == id, cancellationToken);
    }

    public async Task<List<Note>> GetNotesAsync(Guid userId, string? search, string sortBy, string sortOrder, CancellationToken cancellationToken = default)
    {
        var dbQuery = _context.Notes
            .AsNoTracking()
            .Where(n => n.UserId == userId);

        if (!string.IsNullOrEmpty(search))
        {
            dbQuery = dbQuery.Where(n =>
                    EF.Functions.ILike(n.Title, $"%{search}%") ||
                    EF.Functions.ILike(n.Content, $"%{search}%") ||
                    (n.Summary != null && EF.Functions.ILike(n.Summary, $"%{search}%"))
                );
        }

        var sortByLower = sortBy?.ToLowerInvariant();
        var isAscending = sortOrder?.ToLowerInvariant() == "asc";

        switch ((sortByLower, isAscending))
        {
            case ("title", true):
                dbQuery = dbQuery.OrderBy(n => n.Title);
                break;
            case ("title", false):
                dbQuery = dbQuery.OrderByDescending(n => n.Title);
                break;
            case (_, true):
                dbQuery = dbQuery.OrderBy(n => n.UpdateAt);
                break;
            default:
                dbQuery = dbQuery.OrderByDescending(n => n.UpdateAt);
                break;
        }

        return await dbQuery
            .Select(n => new Note
            {
                Id = n.Id,
                Title = n.Title,
                Summary = n.Summary,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }
}
