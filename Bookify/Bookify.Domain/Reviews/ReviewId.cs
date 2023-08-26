using Bookify.Domain.Users;

namespace Bookify.Domain.Reviews;

public sealed record ReviewId(Guid Value)
{
    public static ReviewId New() => new(Guid.NewGuid());
}