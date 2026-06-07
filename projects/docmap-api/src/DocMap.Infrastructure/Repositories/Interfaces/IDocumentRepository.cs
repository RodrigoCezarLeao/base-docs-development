using DocMap.Domain.Models;
using DocMap.Infrastructure.Repositories.Base;

namespace DocMap.Infrastructure.Repositories.Interfaces;

public interface IDocumentRepository : IBaseRepository<Document, int>
{
    Task<IEnumerable<Document>> GetAllByProjectIdAsync(int projectId, CancellationToken cancellationToken = default);
    Task<bool> UpdatePositionAsync(int id, double canvasX, double canvasY, CancellationToken cancellationToken = default);
}
