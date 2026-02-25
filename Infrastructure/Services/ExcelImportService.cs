using Application.Dtos;
using Application.Features.Admin;
using Core.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using MiniExcelLibs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class EmployeeImportService : IEmployeeImportService
    {
        private readonly AppDbContext _context;

        public EmployeeImportService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ImportEmployeesResultDto> ImportAsync(Stream fileStream, int importedByUserId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            var rawRows = MiniExcel.Query(fileStream, useHeaderRow: false).ToList();

            var employees = new List<ImportEmployeeModel>();

            for (int i = 3; i < rawRows.Count; i++)
            {
                var row = rawRows[i] as IDictionary<string, object>;
                if (row == null || row.Values.All(v => v == null))
                    continue;

                var values = row.Values.ToList();

                employees.Add(new ImportEmployeeModel
                {
                    PayrollNumber = Convert.ToInt32(values[0]),
                    FullName = values[1]?.ToString()?.Trim() ?? "",
                    Department = values[2]?.ToString()?.Trim() ?? "",
                    HireDate = Convert.ToDateTime(values[3]),
                    Vacation2024 = values[6] != null ? Convert.ToInt32(values[6]) : 0,
                    Vacation2025 = values[7] != null ? Convert.ToInt32(values[7]) : 0,
                    Vacation2026 = values[8] != null ? Convert.ToInt32(values[8]) : 0
                });
            }

            var payrolls = employees.Select(e => e.PayrollNumber).ToList();

            var existingUsers = await _context.Users
                .Include(u => u.EmployeeProfile)
                .Where(u => payrolls.Contains(u.PayRollNumber))
                .ToListAsync();

            var departmentNames = employees.Select(e => e.Department).Distinct().ToList();

            var existingDepartments = await _context.Departments
                .Where(d => departmentNames.Contains(d.Name))
                .ToListAsync();

            var newDepartments = departmentNames
                .Where(d => !existingDepartments.Any(ed => ed.Name == d))
                .Select(d => new Department
                {
                    Name = d,
                    IsActive = true
                })
                .ToList();

            if (newDepartments.Any())
            {
                _context.Departments.AddRange(newDepartments);
                await _context.SaveChangesAsync();
                existingDepartments.AddRange(newDepartments);
            }

            var newUsers = new List<User>();
            var newProfiles = new List<EmployeeProfile>();
            var newBalances = new List<VacationBalance>();

            int inserted = 0;
            int updated = 0;

            foreach (var emp in employees)
            {
                var user = existingUsers.FirstOrDefault(u => u.PayRollNumber == emp.PayrollNumber);
                var department = existingDepartments.First(d => d.Name == emp.Department);

                if (user == null)
                {
                    user = new User
                    {
                        PayRollNumber = emp.PayrollNumber,
                        FullName = emp.FullName.Trim(),
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(emp.PayrollNumber.ToString()),
                        RoleId = 2,
                        MustChangePassword = true,
                        IsActive = true
                    };

                    newUsers.Add(user);
                    inserted++;
                }
                else
                {
                    user.FullName = emp.FullName.Trim();
                    updated++;
                }
            }

            if (newUsers.Any())
            {
                _context.Users.AddRange(newUsers);
                await _context.SaveChangesAsync();
            }

            var allUsers = await _context.Users
                .Include(u => u.EmployeeProfile)
                .Where(u => payrolls.Contains(u.PayRollNumber))
                .ToListAsync();

            foreach (var emp in employees)
            {
                var user = allUsers.First(u => u.PayRollNumber == emp.PayrollNumber);
                var department = existingDepartments.First(d => d.Name == emp.Department);

                if (user.EmployeeProfile == null)
                {
                    newProfiles.Add(new EmployeeProfile
                    {
                        UserId = user.Id,
                        DepartmentId = department.Id,
                        HireDate = emp.HireDate
                    });
                }

                AddBalanceIfNotExists(newBalances, user.Id, 2024, emp.Vacation2024);
                AddBalanceIfNotExists(newBalances, user.Id, 2025, emp.Vacation2025);
                AddBalanceIfNotExists(newBalances, user.Id, 2026, emp.Vacation2026);
            }

            if (newProfiles.Any())
                _context.EmployeeProfiles.AddRange(newProfiles);

            if (newBalances.Any())
                _context.VacationBalances.AddRange(newBalances);

            await _context.SaveChangesAsync();

            _context.ImportLogs.Add(new ImportLog
            {
                FileName = "VACACIONES 2025 ADMINISTRACION.xlsx",
                ImportedBy = importedByUserId,
                RecordsProcessed = employees.Count,
                RecordsInserted = inserted,
                RecordsUpdated = updated
            });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return new ImportEmployeesResultDto
            {
                RecordsProcessed = employees.Count,
                RecordsInserted = inserted,
                RecordsUpdated = updated
            };
        }

        private void AddBalanceIfNotExists(List<VacationBalance> balances, int userId, int year, int assignedDays)
        {
            balances.Add(new VacationBalance
            {
                UserId = userId,
                Year = year,
                AssignedDays = assignedDays,
                UsedDays = 0,
            });
        }
    }
}