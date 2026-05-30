using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventifyPro.BLL.DTOs.ReviewDTOs
{
    public record ReviewCreateDto(
      int EventId,
      int Rating,
      string? Comment
  );
}
