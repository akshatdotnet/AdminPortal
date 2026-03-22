using BookingSystem.Core.Features.Bookings;
using FluentValidation;

namespace BookingSystem.Core.Validators;

public class CreateBookingValidator : AbstractValidator<CreateBookingCommand>
{
    public CreateBookingValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("Customer ID is required");

        RuleFor(x => x.VenueId)
            .NotEmpty().WithMessage("Venue ID is required");

        // Only check that a date was actually provided - no future/range restrictions
        // so any date string from Swagger works without timezone confusion
        RuleFor(x => x.SlotDate)
            .NotEmpty().WithMessage("Slot date is required")
            .Must(d => d != default).WithMessage("Slot date is required");

        RuleFor(x => x.GuestCount)
            .InclusiveBetween(1, 500)
            .WithMessage("Guest count must be between 1 and 500");
    }
}

public class ProcessPaymentValidator : AbstractValidator<ProcessPaymentCommand>
{
    public ProcessPaymentValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.CardToken).NotEmpty().MinimumLength(4)
            .WithMessage("Card token must be at least 4 characters. Use 'valid_token' for success or 'fail_card' to test failure.");
    }
}

public class CancelBookingValidator : AbstractValidator<CancelBookingCommand>
{
    public CancelBookingValidator()
    {
        RuleFor(x => x.BookingId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}
