using TemperatureApi.Domain.Models;

namespace TemperatureApi.Application.Interfaces;

public interface IUserRepository : IBaseRepository<User, int>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
}
