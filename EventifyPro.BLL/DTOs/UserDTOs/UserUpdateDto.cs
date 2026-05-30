using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventifyPro.BLL.DTOs.UserDTOs
{
    public record UserUpdateDto(
    string FullName,
    string? ProfileImageUrl
);
}
