using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventifyPro.BLL.DTOs.ScanLogDTOs
{
    public record ScanLogResponseDto(
     int Id,
     int TicketId,
     int EventId,
     int? ActualEventId,
     string ScannedById,
     DateTime ScannedAt,
     byte Result,
     string? Notes
 );
}
