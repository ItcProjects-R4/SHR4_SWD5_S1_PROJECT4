using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventifyPro.BLL.DTOs.WaitingListDTOs
{
    public record WaitingListResponseDto(
    int Id,
    int EventId,
    int TicketTypeId,
    string UserId,
    int QuantityWanted,
    byte Status,
    DateTime JoinedAt,
    DateTime? NotifiedAt,
    DateTime? ExpiresAt
);
}
