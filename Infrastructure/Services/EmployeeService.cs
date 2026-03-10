using Application.Features.Admin;
using Application.Features.Admin.Dtos;
using Application.Features.Email;
using Application.Features.Employee.Dtos;
using Core.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class EmployeeService : IEmployeeService
    {
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;

        public EmployeeService(AppDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public async Task<EmployeeDashboardDto> GetDashboardDataAsync(int userId)
        {
            var currentYear = DateTime.Now.Year;

            var balances = await _context.VacationBalances
                .Where(v => v.UserId == userId)
                .ToListAsync();

            int totalAssigned = balances.Sum(b => b.AssignedDays);
            int totalUsed = balances.Sum(b => b.UsedDays);

            var history = await _context.VacationRequests
                .Include(r => r.Status)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new VacationRequestDto
                {
                    Id = r.Id,
                    StartDate = r.StartDate.ToString("dd MMM yyyy"),
                    EndDate = r.EndDate.ToString("dd MMM yyyy"),
                    Days = r.RequestedDays,
                    Status = r.Status.Name
                })
                .Take(5)
                .ToListAsync();

            return new EmployeeDashboardDto
            {
                TotalDays = totalAssigned,
                UsedDays = totalUsed,
                History = history
            };
        }

        public async Task<int> CreateEmployeeAsync(CreateEmployeeDto dto)
        {
            if(await _context.Users.AnyAsync(u => u.PayRollNumber == dto.PayRollNumber))
            {
                throw new Exception($"El número de nómina {dto.PayRollNumber} ya esta registrado");
            }

            var user = new User
            {
                PayRollNumber = dto.PayRollNumber,
                FullName = dto.FullName.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.PayRollNumber.ToString()),
                RoleId = 2,
                MustChangePassword = true,
                IsActive = true,
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var department = await _context.Departments.FirstOrDefaultAsync(d => d.Name == dto.Department)
                        ?? new Department { Name = dto.Department, IsActive = true };

            var profile = new EmployeeProfile
            {
                UserId = user.Id,
                Department = department,
                HireDate = dto.HireDate
            };

            _context.EmployeeProfiles.Add(profile);

            var today = DateTime.Today;

            int yearsOfService = today.Year - dto.HireDate.Year;
            if (dto.HireDate.Date > today.AddYears(-yearsOfService)) yearsOfService--;

            int assignedDays = CalculateVacationDays(yearsOfService);

            var initialBalance = new VacationBalance
            {
                UserId = user.Id,
                Year = today.Year,
                AssignedDays = assignedDays,
                UsedDays = 0,
            };

            _context.VacationBalances.Add(initialBalance);

            await _context.SaveChangesAsync();

            return user.Id;
        }

        public async Task<bool> UpdateEmployeeAsync(UpdateEmployeeDto dto)
        {
            var user = await _context.Users
                    .Include(u => u.EmployeeProfile)
                    .Include(u => u.VacationBalances)
                    .FirstOrDefaultAsync(u => u.Id == dto.Id);

            if (user == null) return false;

            if (dto.PayRollNumber.HasValue && dto.PayRollNumber > 0 && user.PayRollNumber != dto.PayRollNumber)
            {
                if (await _context.Users.AnyAsync(u => u.PayRollNumber == dto.PayRollNumber && u.Id != dto.Id))
                    throw new Exception("El número de nómina ya existe.");

                user.PayRollNumber = dto.PayRollNumber.Value;
            }

            if (!string.IsNullOrWhiteSpace(dto.FullName))
                user.FullName = dto.FullName.Trim();

            if (dto.IsActive.HasValue)
                user.IsActive = dto.IsActive.Value;

            if (!string.IsNullOrWhiteSpace(dto.Department) || dto.HireDate.HasValue)
            {
                if (user.EmployeeProfile == null)
                {
                    user.EmployeeProfile = new EmployeeProfile { UserId = user.Id };
                    _context.EmployeeProfiles.Add(user.EmployeeProfile);
                }

                if (!string.IsNullOrWhiteSpace(dto.Department))
                {
                    var dept = await _context.Departments.FirstOrDefaultAsync(d => d.Name == dto.Department);

                    if (dept == null)
                    {
                        dept = new Department { Name = dto.Department, IsActive = true };
                        _context.Departments.Add(dept);
                        await _context.SaveChangesAsync();
                    }

                    user.EmployeeProfile.DepartmentId = dept.Id;
                }

                if (dto.HireDate.HasValue && dto.HireDate != default)
                {
                    user.EmployeeProfile.HireDate = dto.HireDate.Value;
                }
            }

            if (dto.Balances != null && dto.Balances.Any())
            {
                foreach (var bDto in dto.Balances)
                {
                    var existing = user.VacationBalances.FirstOrDefault(b => b.Year == bDto.Year);
                    if (existing != null)
                        existing.AssignedDays = bDto.AssignedDays;
                    else
                        user.VacationBalances.Add(new VacationBalance
                        {
                            Year = bDto.Year,
                            AssignedDays = bDto.AssignedDays,
                            UserId = user.Id
                        });
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteEmployeeAsync(int id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);

            if (user == null) return false;

            user.IsActive = false;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> ReactivateEmployeeAsync(int id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);

            if (user == null) return false;

            user.IsActive = true;

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> RequestVacationAsync(int userId, CreateVacationRequestDto dto)
        {
            var employee = await _context.Users
                    .Include(u => u.EmployeeProfile)
                    .ThenInclude(p => p.Manager)
                    .FirstOrDefaultAsync(u => u.Id == userId);

            if (employee?.EmployeeProfile?.Manager == null)
                throw new Exception("No se encontró un jefe directo asignado");

            var balance = await _context.VacationBalances
                        .Where(b => b.UserId == userId && b.Year == DateTime.Now.Year)
                        .FirstOrDefaultAsync();

            if (balance == null || (balance.AssignedDays - balance.UsedDays) < dto.RequestedDays)
                throw new Exception("Días insuficientes en el balance");

            var request = new VacationRequest
            {
                UserId = userId,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                RequestedDays = dto.RequestedDays,
                StatusId = 1
            };

            _context.VacationRequests.Add(request);
            await _context.SaveChangesAsync();

            var approvel = new VacationRequestApproval
            {
                VacationRequestId = request.Id,
                ApproverId = employee.EmployeeProfile.ManagerId!.Value,
                ApprovalLevel = 1,
                StatusId = 1
            };

            _context.VacationRequestApprovals.Add(approvel);
            await _context.SaveChangesAsync();

            string subject = "Nueva Solicitud de Vacaciones - MESA";

            string body = $@"
            <h3>Hola, {employee.EmployeeProfile.Manager.FullName}</h3>
            <p>El colaborador <b>{employee.FullName}</b> ha solicitado <b>{dto.RequestedDays}</b> días de vacaciones.</p>
            <p>Periodo: del {dto.StartDate:dd/MM/yyyy} al {dto.EndDate:dd/MM/yyyy}.</p>
            <br>
            <a href='https://tuportal.mesa.com/aprobaciones'>Haga clic aquí para revisar y autorizar</a>";

            await _emailService.SendEmailAsync(new[] { employee.EmployeeProfile.Manager.Email }!, subject, body);

            return true;
        }

        private int CalculateVacationDays(int years)
        {
            if (years < 1) return 0;

            if(years <= 5)
            {
                return 12 + ((years - 1) * 2);
            }

            int fivePeriod = (int)Math.Floor((years - 1) / 5.0);

            return 20 + (fivePeriod * 2);
        }
    }
}