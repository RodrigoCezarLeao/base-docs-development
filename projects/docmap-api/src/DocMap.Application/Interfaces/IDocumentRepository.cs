using DocMap.Domain.Models;

namespace DocMap.Application.Interfaces;

public interface IDocumentRepository : IBaseRepository<Document, int>
{
    Task<IEnumerable<Document>> GetAllByProjectIdAsync(int projectId, CancellationToken cancellationToken = default);
    Task<bool> UpdatePositionAsync(int id, double canvasX, double canvasY, CancellationToken cancellationToken = default);
}
