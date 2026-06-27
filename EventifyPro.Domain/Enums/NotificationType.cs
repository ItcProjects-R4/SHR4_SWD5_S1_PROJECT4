namespace Eventify.Domain.Enums;

public enum NotificationType : byte
{
    BookingConfirmed = 0,
    PaymentFailed = 1,
    EventTomorrow = 2,
    TicketScanned = 3,
    ReviewReminder = 4,
    RefundStatus = 5,
    Maintenance = 6,
    SystemUpdate = 7,
    CustomAlert = 8
}
