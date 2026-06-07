using DocMap.Domain.Models;

namespace DocMap.Application.Interfaces;

public interface IDocumentConnectionRepository : IBaseRepository<DocumentConnection, int>
{
    Task<IEnumerable<DocumentConnection>> GetAllByProjectIdAsync(int projectId, CancellationToken cancellationToken = default);
    Task<DocumentConnection?> GetBySourceAndTargetAsync(int sourceDocumentId, int targetDocumentId, CancellationToken cancellationToken = default);
}
