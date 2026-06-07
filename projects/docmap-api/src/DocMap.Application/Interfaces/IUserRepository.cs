using DocMap.Domain.Models;

namespace DocMap.Application.Interfaces;

public interface IUserRepository : IBaseRepository<User, int>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
}
