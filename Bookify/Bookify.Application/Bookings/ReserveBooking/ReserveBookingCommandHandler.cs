using Bookify.Application.Abstractions.Clock;
using Bookify.Application.Abstractions.Messaging;
using Bookify.Application.Exceptions;
using Bookify.Domain.Abstractions;
using Bookify.Domain.Apartments;
using Bookify.Domain.Bookings;
using Bookify.Domain.Users;

namespace Bookify.Application.Bookings.ReserveBooking;
public sealed class ReserveBookingCommandHandler : ICommandHandler<ReserveBookingCommand, BookingId>
{
    private readonly IUserRepository _userRepository;
    private readonly IApartmentRepository _apartmentRepository;
    private readonly IBookingRepository _bookingRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly PricingService _pricingService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ReserveBookingCommandHandler(
        IUserRepository userRepository,
        IApartmentRepository apartmentRepository,
        IBookingRepository bookingRepository,
        IUnitOfWork unitOfWork,
        PricingService pricingService,
        IDateTimeProvider dateTimeProvider)
    {
        _userRepository = userRepository;
        _apartmentRepository = apartmentRepository;
        _bookingRepository = bookingRepository;
        _unitOfWork = unitOfWork;
        _pricingService = pricingService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<BookingId>> Handle(ReserveBookingCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failiure<BookingId>(UserErrors.NotFound);
        }

        var apartment = await _apartmentRepository.GetByIdAsync(request.AppartmentId, cancellationToken);

        if (apartment is null)
        {
            return Result.Failiure<BookingId>(ApartmentErrors.NotFound);
        }

        var duration = DateRange.Create(request.StartDate, request.EndDate);

        // Race condition
        // One thread could get that the booking is available and before it reserves it
        // another thread asks if the same booking is available at the same time and get the same response
        if (await _bookingRepository.IsOverlappingAsync(apartment, duration, cancellationToken))
        {
            return Result.Failiure<BookingId>(BookingErrors.Overlap);
        }

        // SOLUTION - Optimistic Concurrency - uses a column in the DB that represents record version
        // if version in memomory and in the DB are different the record has changed
        // At UnitOfWork lvl we handle concurrency 
        try
        {
            var booking = Booking.Reserve(
            apartment,
            user.Id,
            duration,
            _dateTimeProvider.UtcNow,
            _pricingService);

            _bookingRepository.Add(booking);
            await _unitOfWork.SaveChangesAsync();

            return booking.Id;
        }
        catch (ConcurrencyException)
        {
            return Result.Failiure<BookingId>(BookingErrors.Overlap);
        }

    }
}
