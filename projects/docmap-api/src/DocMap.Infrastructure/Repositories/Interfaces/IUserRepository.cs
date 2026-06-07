using DocMap.Domain.Models;
using DocMap.Infrastructure.Repositories.Base;

namespace DocMap.Infrastructure.Repositories.Interfaces;

public interface IUserRepository : IBaseRepository<User, int>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
}
