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

    public async Task<Note?> UpdateNoteAsync(Guid id, string title, string content, string? summary, CancellationToken cancellationToken = default)
    {
        var note = await _context.Notes.FindAsync([id], cancellationToken);
        if (note is null)
            return null;

        note.Title = title;
        note.Content = content;
        note.Summary = summary;
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

    public async Task<List<Note>> GetNotesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Notes
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.UpdateAt)
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
