using TinyNote.Api.Data.Entities;

namespace TinyNote.Api.Repository;

public interface IUserRepository
{
    Task<User> AddAsync(User user, CancellationToken cancellationToken = default);
}
