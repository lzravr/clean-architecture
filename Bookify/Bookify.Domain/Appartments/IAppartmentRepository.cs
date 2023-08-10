using Bookify.Domain.Users;

namespace Bookify.Domain.Appartments;
public interface IAppartmentRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
