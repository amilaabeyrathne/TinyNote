using TinyNote.Api.Data;
using TinyNote.Api.Data.Entities;

namespace TinyNote.Api.Repository;

public class UserRepository : IUserRepository
{
    private readonly NotesDbContext _context;

    public UserRepository(NotesDbContext context)
    {
        _context = context;
    }

    public async Task<User> AddAsync(User user, CancellationToken cancellationToken = default)
    {
        user.Id = Guid.NewGuid();
        user.CreatedAt = DateTimeOffset.UtcNow;

        await _context.Users.AddAsync(user, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return user;
    }
}
