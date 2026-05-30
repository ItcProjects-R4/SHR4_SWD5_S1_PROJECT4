using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventifyPro.BLL.DTOs.DashboardDTOs
{
    public record AdminDashboardDto(
     int TotalUsers,
     int TotalEvents,
     int TotalBookings,
     decimal TotalRevenue
 );

}
