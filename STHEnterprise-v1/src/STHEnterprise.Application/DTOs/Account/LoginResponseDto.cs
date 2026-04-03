using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace STHEnterprise.Application.DTOs.Account
{
    public sealed class LoginResponseDto
    {
        public string Token { get; init; } = string.Empty;
        public UserDto User { get; init; } = default!;
    }

}
