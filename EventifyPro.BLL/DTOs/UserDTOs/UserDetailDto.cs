using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventifyPro.BLL.DTOs.UserDTOs
{
    public record UserDetailDto(
     string Id,
     string FullName,
     string? ProfileImageUrl,
     bool IsActive,
     DateTime CreatedAt
 );
}
