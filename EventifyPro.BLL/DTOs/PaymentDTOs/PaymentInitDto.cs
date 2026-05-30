using Eventify.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventifyPro.BLL.DTOs.PaymentDTOs
{

    public record PaymentInitDto(
        int BookingId,
        PaymentMethod Method
    );
}
