using Eventify.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventifyPro.BLL.DTOs.PaymentDTOs
{
    public record PaymentResultDto(
    int Id,
    int BookingId,
    decimal Amount,
    PaymentMethod Method,
    PaymentStatus Status,
    string? TransactionId,
    DateTime PaymentDate
);
}
