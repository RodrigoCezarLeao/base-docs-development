using DocMap.Domain.Models;
using DocMap.Infrastructure.Repositories.Base;

namespace DocMap.Infrastructure.Repositories.Interfaces;

public interface IProjectRepository : IBaseRepository<Project, int>
{
    Task<IEnumerable<Project>> GetAllByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<int> GetDocumentCountAsync(int projectId, CancellationToken cancellationToken = default);
}
