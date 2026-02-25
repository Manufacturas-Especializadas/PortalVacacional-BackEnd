using Application.Dtos.Auth;
using Application.Features.Security;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class AuthService
    {
        private readonly AppDbContext _context;
        private readonly ITokenService _tokenService;

        public AuthService(AppDbContext context, ITokenService tokenService)
        {
            _context = context;
            _tokenService = tokenService;
        }

        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.PayRollNumber == request.PayRollNumber);

            if (user == null)
                throw new Exception("User not found");

            var valid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);

            if (!valid)
                throw new Exception("Invalid credentials");

            var token = _tokenService.GenerateToken(user);

            return new LoginResponse
            {
                Token = token,
                FullName = user.FullName,
                Role = user.Role.Name,
                MustChangePassword = user.MustChangePassword
            };
        }
    }
}