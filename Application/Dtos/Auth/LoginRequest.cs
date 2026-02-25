using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dtos.Auth
{
    public class LoginRequest
    {
        public int PayRollNumber { get; set; }

        public string Password { get; set; }
    }
}