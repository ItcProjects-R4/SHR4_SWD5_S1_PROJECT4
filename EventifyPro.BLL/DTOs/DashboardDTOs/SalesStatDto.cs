using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventifyPro.BLL.DTOs.DashboardDTOs
{
    public record SalesStatDto(
     int EventId,
     string EventTitle,
     decimal Revenue,
     int TicketsSold
 );
}
