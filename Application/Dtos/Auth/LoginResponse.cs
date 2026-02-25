using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dtos.Auth
{
    public class LoginResponse
    {
        public string Token { get; set; } = null!;

        public string FullName { get; set; } = null!;

        public string Role {  get; set; } = null!;

        public bool MustChangePassword { get; set; }
    }
}