using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eventify.BLL.DTOs.TicketDTOs;

public record TicketDetailsDto
(
    int Id,
    string QRCode,
    bool IsUsed,
    DateTime? UsedAt
);