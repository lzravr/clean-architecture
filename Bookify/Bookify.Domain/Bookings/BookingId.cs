using Bookify.Domain.Users;

namespace Bookify.Domain.Bookings;

public sealed record BookingId(Guid Value)
{
    public static BookingId New() => new(Guid.NewGuid());
}