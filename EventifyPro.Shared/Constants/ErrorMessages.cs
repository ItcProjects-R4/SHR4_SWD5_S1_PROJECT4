namespace Eventify.Shared.Constants;

/// <summary>
/// All user-facing error messages in one place.
/// Principle: No magic strings in BLL or Controllers — all messages come from here.
///
/// Organization: Nested static class per domain.
/// Usage examples:
///   return Result.Failure(ErrorMessages.Event.NotFound);
///   return Result.Failure(ErrorMessages.Booking.InsufficientCapacity);
/// </summary>
public static class ErrorMessages
{
    public static class General
    {
        public const string NotFound = "The requested item was not found.";
        public const string Unauthorized = "You are not authorized to perform this action.";
        public const string ServerError = "An unexpected server error occurred. Please try again later.";
        public const string InvalidOperation = "The requested operation is not allowed in the current state.";
        public const string ConcurrencyConflict = "A data conflict occurred. Please refresh and try again.";
        public const string ValidationFailed = "The provided data is invalid.";
    }

    public static class User
    {
        public const string NotFound = "User not found.";
        public const string AlreadyExists = "This email address is already in use.";
        public const string InvalidCredentials = "Invalid email or password.";
        public const string AccountDisabled = "Your account has been disabled. Please contact support.";
        public const string CannotDeactivateSelf = "You cannot deactivate your own account.";
        public const string CannotDowngradeLastAdmin = "Cannot downgrade the last administrator in the system.";
        public const string RoleNotFound = "The specified role was not found.";
        public const string RoleAssignmentFailed = "Failed to assign role. Please try again.";
    }

    public static class Event
    {
        public const string NotFound = "Event not found.";
        public const string NotPublished = "This event is not published and cannot be booked.";
        public const string AlreadyCancelled = "This event has already been cancelled.";
        public const string AlreadyStarted = "An event that has already started cannot be modified.";
        public const string StartDateMustBeFuture = "The start date must be in the future.";
        public const string EndDateMustBeAfterStart = "The end date must be after the start date.";
        public const string NotDraftForPublish = "Only draft events can be published.";
        public const string NotOwnedByOrganizer = "You cannot modify an event you do not own.";
        public const string CapacityExceeded = "The total quantity of ticket types exceeds the event capacity.";
        public const string RequiresTicketType = "At least one ticket type must be added before publishing.";
        public const string CategoryLinked = "Cannot delete a category linked to existing events.";
    }

    public static class TicketType
    {
        public const string NotFound = "Ticket type not found.";
        public const string DuplicateName = "A ticket type with this name already exists for this event.";
        public const string CannotReduceBelowSold = "Cannot reduce quantity below the number already sold ({0}).";
        public const string PriceMustBeNonNegative = "Ticket price must be zero or greater.";
        public const string QuantityMustBePositive = "Ticket quantity must be greater than zero.";
        public const string SaleWindowInvalid = "Sale end date must be after the sale start date.";
        public const string SaleWindowClosed = "The sales window for this ticket type is currently closed.";
        public const string SoldOut = "The selected ticket type is sold out.";
        public const string InsufficientStock = "Requested quantity ({0}) is not available. Available: {1}.";
    }

    public static class Booking
    {
        public const string NotFound = "Booking not found.";
        public const string NotOwnedByUser = "You cannot access a booking that does not belong to you.";
        public const string AlreadyConfirmed = "This booking has already been confirmed.";
        public const string AlreadyCancelled = "This booking has already been cancelled.";
        public const string NotConfirmedForCancel = "Only confirmed bookings can be cancelled.";
        public const string CancellationWindowPassed = "The cancellation window has expired. Bookings cannot be cancelled less than 24 hours before the event starts.";
        public const string EmptyItems = "At least one ticket type must be selected.";
        public const string DuplicateTicketType = "Duplicate ticket types are not allowed in the same booking. Increase the quantity instead.";
        public const string QuantityMustBePositive = "Ticket quantity for each type must be greater than zero.";
        public const string ConcurrencyRetryExceeded = "Booking confirmation failed due to high traffic. Please try again.";
        public const string DuplicateBooking = "You already have a confirmed booking for this event.";
    }

    public static class Payment
    {
        public const string NotFound = "Payment record not found.";
        public const string AlreadyCompleted = "Payment has already been completed.";
        public const string Failed = "Payment failed. Please verify your card details and try again.";
        public const string AmountMismatch = "Payment amount does not match the booking total.";
        public const string BookingNotPending = "Only pending bookings can be paid.";
    }

    public static class Refund
    {
        public const string NotFound = "Refund record not found.";
        public const string ExceedsPaymentAmount = "Refund amount exceeds the refundable payment amount.";
        public const string AmountMustBePositive = "Refund amount must be greater than zero.";
        public const string NoPaymentToRefund = "There is no completed payment available for refund for this booking.";
        public const string RefundFailed = "Refund request failed with the payment gateway. Please try again later.";
    }

    public static class Ticket
    {
        public const string NotFound = "Ticket not found.";
        public const string AlreadyUsed = "This ticket has already been used.";
        public const string InvalidQRCode = "Invalid or corrupted QR code.";
        public const string WrongEvent = "This ticket belongs to a different event.";
        public const string GenerationFailed = "Failed to generate ticket. Please contact support.";
        public const string PdfGenerationFailed = "Failed to generate PDF file. Please try again.";
    }

    public static class Review
    {
        public const string NotFound = "Review not found.";
        public const string EventNotFinished = "Reviews can only be submitted after the event has ended.";
        public const string NoConfirmedBooking = "You must have a confirmed booking for this event to submit a review.";
        public const string AlreadyReviewed = "You have already reviewed this event.";
        public const string RatingOutOfRange = "Rating must be between 1 and 5.";
        public const string NotOwnedByUser = "You cannot delete a review that does not belong to you.";
    }

    public static class WaitingList
    {
        public const string NotFound = "Waiting list entry not found.";
        public const string AlreadyJoined = "You are already on the waiting list for this ticket type.";
        public const string TicketStillAvailable = "Tickets are still available. You can book directly.";
        public const string QuantityMustBePositive = "Requested quantity must be greater than zero.";
        public const string TicketTypeNotInEvent = "The selected ticket type does not belong to this event.";
        public const string NotificationExpired = "The response window for the seat availability notification has expired.";
        public const string NotOwnedByUser = "You cannot access a waiting list entry that does not belong to you.";
    }

    public static class Scanner
    {
        public const string NotAuthorized = "This account is not authorized to scan tickets.";
        public const string ValidationTimeout = "Validation took too long. Please try again.";
    }

  
    public static class Category
    {
        public const string NotFound = "Category not found.";
        public const string NameAlreadyExists = "This category name is already in use.";
        public const string CannotDeleteLinked = "Cannot delete a category linked to existing events.";
    }

    public static class Email
    {
        public const string SendFailed = "Failed to send email. It will be retried automatically.";
        public const string MaxRetriesExceeded = "The email exceeded the maximum retry attempts.";
    }
}