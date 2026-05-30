using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventifyPro.BLL.DTOs.RefundDTOs
{

    public record RefundCreateDto(
        int BookingId,
        decimal Amount,
        string? Reason
    );
}
