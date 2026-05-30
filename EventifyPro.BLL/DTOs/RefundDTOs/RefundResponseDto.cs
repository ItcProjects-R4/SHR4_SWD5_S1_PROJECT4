using Eventify.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventifyPro.BLL.DTOs.RefundDTOs
{
    public record RefundResponseDto(
      int Id,
      int PaymentId,
      int BookingId,
      decimal Amount,
      RefundStatus Status,
      string? TransactionId,
      string? Reason,
      string InitiatedById,
      DateTime CreatedAt,
      DateTime? ProcessedAt
  );
}
