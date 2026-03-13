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
            if (await _context.Users.AnyAsync(u => u.PayRollNumber == dto.PayRollNumber))
            {
                throw new Exception($"El número de nómina {dto.PayRollNumber} ya está registrado");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var user = new User
                {
                    PayRollNumber = dto.PayRollNumber,
                    FullName = dto.FullName.Trim(),
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.PayRollNumber.ToString()),
                    RoleId = (dto.RoleId.HasValue && dto.RoleId.Value > 0) ? dto.RoleId.Value : 2,
                    MustChangePassword = true,
                    IsActive = true,
                    Email = dto.Email
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                var department = await _context.Departments.FirstOrDefaultAsync(d => d.Name == dto.Department)
                                ?? new Department { Name = dto.Department, IsActive = true };

                if (department.Id == 0)
                {
                    _context.Departments.Add(department);
                    await _context.SaveChangesAsync();
                }

                var profile = new EmployeeProfile
                {
                    UserId = user.Id,
                    DepartmentId = department.Id,
                    HireDate = dto.HireDate,
                    ManagerId = dto.ManagerId > 0 ? dto.ManagerId : null
                };
                _context.EmployeeProfiles.Add(profile);

                var authorityRoles = new List<int> { 3, 4 };
                if (authorityRoles.Contains(user.RoleId))
                {
                    _context.Managers.Add(new Manager
                    {
                        PayRollNumber = user.PayRollNumber,
                        FullName = user.FullName,
                        Email = string.IsNullOrWhiteSpace(user.Email) ? null : user.Email,
                        DepartmentId = department.Id,
                        RoleId = user.RoleId,
                        IsActive = true
                    });
                }

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
                await transaction.CommitAsync();

                return user.Id;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> UpdateEmployeeAsync(UpdateEmployeeDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var user = await _context.Users
                    .Include(u => u.EmployeeProfile)
                    .Include(u => u.VacationBalances)
                    .FirstOrDefaultAsync(u => u.Id == dto.Id);

                if (user == null) return false;

                int oldRoleId = user.RoleId;

                if (dto.PayRollNumber.HasValue && dto.PayRollNumber > 0 && user.PayRollNumber != dto.PayRollNumber)
                {
                    if (await _context.Users.AnyAsync(u => u.PayRollNumber == dto.PayRollNumber && u.Id != dto.Id))
                        throw new Exception("El número de nómina ya existe.");

                    user.PayRollNumber = dto.PayRollNumber.Value;
                }

                if (dto.RoleId.HasValue && dto.RoleId.Value != oldRoleId)
                {
                    user.RoleId = dto.RoleId.Value;
                    var authorityRoles = new List<int> { 3, 4 };
                    bool isNowAuthority = authorityRoles.Contains(dto.RoleId.Value);
                    bool wasAuthority = authorityRoles.Contains(oldRoleId);

                    if (isNowAuthority)
                    {
                        var managerEntry = await _context.Managers
                            .FirstOrDefaultAsync(m => m.PayRollNumber == user.PayRollNumber);

                        if (managerEntry == null)
                        {
                            _context.Managers.Add(new Manager
                            {
                                PayRollNumber = user.PayRollNumber,
                                FullName = user.FullName,
                                Email = user.Email ?? "",
                                DepartmentId = user.EmployeeProfile?.DepartmentId ?? 1,
                                RoleId = dto.RoleId.Value,
                                IsActive = true
                            });
                        }
                        else
                        {
                            managerEntry.RoleId = dto.RoleId.Value;
                            managerEntry.IsActive = true;
                        }
                    }
                    else if (wasAuthority && !isNowAuthority)
                    {
                        var managerEntry = await _context.Managers.FirstOrDefaultAsync(m => m.PayRollNumber == user.PayRollNumber);
                        if (managerEntry != null) _context.Managers.Remove(managerEntry);
                    }
                }

                if (!string.IsNullOrWhiteSpace(dto.FullName)) user.FullName = dto.FullName.Trim();
                if (dto.IsActive.HasValue) user.IsActive = dto.IsActive.Value;

                if (!string.IsNullOrWhiteSpace(dto.Department) || dto.HireDate.HasValue || dto.ManagerId.HasValue)
                {
                    if (user.EmployeeProfile == null)
                    {
                        user.EmployeeProfile = new EmployeeProfile { UserId = user.Id, HireDate = dto.HireDate ?? DateTime.Today };
                        _context.EmployeeProfiles.Add(user.EmployeeProfile);
                    }

                    user.EmployeeProfile.ManagerId = dto.ManagerId;

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

                if (dto.Balances != null)
                {
                    foreach (var bDto in dto.Balances)
                    {
                        var existing = user.VacationBalances.FirstOrDefault(b => b.Year == bDto.Year);
                        if (existing != null) existing.AssignedDays = bDto.AssignedDays;
                        else user.VacationBalances.Add(new VacationBalance { Year = bDto.Year, AssignedDays = bDto.AssignedDays, UserId = user.Id });
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
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
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var employee = await _context.Users
                        .Include(u => u.EmployeeProfile)
                        .FirstOrDefaultAsync(u => u.Id == userId);

                if (employee?.EmployeeProfile == null || !employee.EmployeeProfile.ManagerId.HasValue)
                    throw new Exception("No se encontró un jefe directo asignado en el perfil.");

                var manager = await _context.Managers
                        .FirstOrDefaultAsync(m => m.Id == employee.EmployeeProfile.ManagerId);

                if (manager == null)
                    throw new Exception("El jefe asignado no existe en el catálogo de Managers.");

                var balance = await _context.VacationBalances
                            .Where(b => b.UserId == userId && b.Year == DateTime.Now.Year)
                            .FirstOrDefaultAsync();

                if (balance == null || (balance.AssignedDays - balance.UsedDays) < dto.RequestedDays)
                    throw new Exception("Días insuficientes en el balance actual.");

                var request = new VacationRequest
                {
                    UserId = userId,
                    StartDate = dto.StartDate,
                    EndDate = dto.EndDate,
                    RequestedDays = dto.RequestedDays,
                    StatusId = 1,
                    CreatedAt = DateTime.Now
                };

                _context.VacationRequests.Add(request);
                await _context.SaveChangesAsync();

                var approval = new VacationRequestApproval
                {
                    VacationRequestId = request.Id,                    
                    ApproverId = manager.Id,
                    ApprovalLevel = 1,
                    StatusId = 1
                };

                _context.VacationRequestApprovals.Add(approval);

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                if (!string.IsNullOrEmpty(manager.Email))
                {
                    await NotifyManager(manager.Email, manager.FullName, employee.FullName, dto);
                }

                return true;
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();
                var innerMessage = ex.InnerException?.Message ?? ex.Message;
                throw new Exception($"Error de base de datos: {innerMessage}");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw;
            }
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

        private async Task NotifyManager(string managerEmail, string managerName, string employeeName, CreateVacationRequestDto dto)
        {
            string subject = $"MESA - Nueva Solicitud de Vacaciones: {employeeName}";
            string body = $@"
                <div style='font-family: sans-serif; max-width: 600px; border: 1px solid #e2e8f0; padding: 25px; border-radius: 15px;'>
                    <h2 style='color: #1e40af;'>Hola, {managerName}</h2>
                    <p style='font-size: 16px;'>Tienes una nueva solicitud de vacaciones pendiente de revisar en el portal.</p>
                    <div style='background-color: #f8fafc; padding: 15px; border-radius: 10px; border-left: 5px solid #3b82f6;'>
                        <p style='margin: 5px 0;'><b>Colaborador:</b> {employeeName}</p>
                        <p style='margin: 5px 0;'><b>Periodo:</b> {dto.StartDate:dd/MM/yyyy} al {dto.EndDate:dd/MM/yyyy}</p>
                        <p style='margin: 5px 0;'><b>Días totales:</b> {dto.RequestedDays} días</p>
                    </div>
                    <br>
                    <div style='text-align: center;'>
                        <a href='https://tuportal.mesa.com/approvals' 
                           style='background-color: #2563eb; color: white; padding: 12px 25px; text-decoration: none; border-radius: 8px; font-weight: bold;'>
                           Revisar en el Portal
                        </a>
                    </div>
                    <p style='font-size: 12px; color: #94a3b8; margin-top: 25px;'>Este es un mensaje automático generado por el sistema MESA.</p>
                </div>";

            await _emailService.SendEmailAsync(new[] { managerEmail }, subject, body);
        }
    }
}