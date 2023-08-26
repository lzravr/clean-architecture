namespace Bookify.Domain.Users;

public sealed record UserId(Guid Value)
{
    public static UserId New() => new(Guid.NewGuid());
}