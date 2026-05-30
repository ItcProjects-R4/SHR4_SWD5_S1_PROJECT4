using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eventify.BLL.DTOs.BookingItemDTOs;

public record CreateBookingItemDto
(
    int TicketTypeId,
    int Quantity
);