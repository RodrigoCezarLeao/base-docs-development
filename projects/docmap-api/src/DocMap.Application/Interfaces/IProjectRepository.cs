using DocMap.Domain.Models;

namespace DocMap.Application.Interfaces;

public interface IProjectRepository : IBaseRepository<Project, int>
{
    Task<IEnumerable<Project>> GetAllByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<int> GetDocumentCountAsync(int projectId, CancellationToken cancellationToken = default);
}
