using Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Security
{
    public interface ITokenService
    {
        string GenerateToken(User user);
    }
}