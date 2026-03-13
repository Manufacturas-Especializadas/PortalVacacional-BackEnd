using Application.Features.Email;
using Application.Features.Employee;
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
    public class VacationService : IVacationService
    {
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;

        public VacationService(AppDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public async Task<List<PendingRequestDto>> GetPendingRequestsForManagerAsync(int managerPayroll)
        {
            var manager = await _context.Managers
                .FirstOrDefaultAsync(m => m.PayRollNumber == managerPayroll);

            if (manager == null) return new List<PendingRequestDto>();

            return await _context.VacationRequestApprovals
                .Include(a => a.VacationRequest)
                    .ThenInclude(r => r.User)
                .Where(a => a.ApproverId == manager.Id && a.StatusId == 1)
                .Select(a => new PendingRequestDto
                {
                    RequestId = a.VacationRequestId,
                    EmployeeName = a.VacationRequest.User.FullName,
                    PayrollNumber = a.VacationRequest.User.PayRollNumber,
                    StartDate = a.VacationRequest.StartDate.ToString("dd/MM/yyyy"),
                    EndDate = a.VacationRequest.EndDate.ToString("dd/MM/yyyy"),
                    DaysRequested = a.VacationRequest.RequestedDays,
                    RequestedAt = a.VacationRequest.CreatedAt.ToString("dd/MM/yyyy HH:mm")
                })
                .ToListAsync();
        }

        public async Task<bool> ProcessApprovalAsync(int managerPayroll, ProcessApprovalDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var manager = await _context.Managers
                    .FirstOrDefaultAsync(m => m.PayRollNumber == managerPayroll);

                if (manager == null) throw new Exception("Manager no identificado");

                var approval = await _context.VacationRequestApprovals
                                    .Include(a => a.VacationRequest)
                                    .FirstOrDefaultAsync(a => a.VacationRequestId == dto.RequestId
                                                     && a.ApproverId == manager.Id
                                                     && a.StatusId == 1);

                if (approval == null) throw new Exception("No se encontró la solicitud pendiente");

                approval.StatusId = dto.Approved ? 2 : 3;
                approval.Comments = dto.Comments;
                approval.DecisionDate = DateTime.Now;

                var request = approval.VacationRequest;
                request.StatusId = approval.StatusId;

                if (dto.Approved)
                {
                    var balance = await _context.VacationBalances
                                        .FirstOrDefaultAsync(b => b.UserId == request.UserId
                                                             && b.Year == request.StartDate.Year);

                    if (balance == null)
                        throw new Exception($"No se encontró el balance para el año {request.StartDate.Year}");

                    if ((balance.AssignedDays - balance.UsedDays) < request.RequestedDays)
                        throw new Exception("El empleado ya no cuenta con días suficientes");

                    balance.UsedDays += request.RequestedDays;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                try
                {
                    await NotifyEmployee(request.UserId, dto.Approved, dto.Comments!);
                }
                catch 
                {

                }

                return true;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private async Task NotifyEmployee(int userId, bool approved, string comments)
        {
            var employee = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (employee == null || string.IsNullOrEmpty(employee.Email)) return;

            string statusText = approved ? "APROBADA" : "RECHAZADA";
            string color = approved ? "#10b981" : "#f43f5e";

            string subject = $"Tu solicitud de vacaciones ha sido {statusText}";

            string body = $@"
                <div style='font-family: sans-serif; max-width: 600px; margin: auto; border: 1px solid #e2e8f0; border-radius: 12px; overflow: hidden;'>
                    <div style='background-color: {color}; padding: 20px; text-align: center; color: white;'>
                        <h1 style='margin: 0;'>Portal MESA</h1>
                    </div>
                    <div style='padding: 30px;'>
                        <h2 style='color: #1e293b;'>Hola, {employee.FullName}</h2>
                        <p style='font-size: 16px; color: #64748b;'>Te informamos que tu solicitud de vacaciones ha sido:</p>
                        <div style='background-color: #f8fafc; border-left: 4px solid {color}; padding: 15px; margin: 20px 0;'>
                            <strong style='font-size: 18px; color: {color};'>{statusText}</strong>
                        </div>
                        <p style='font-size: 14px; color: #1e293b;'><strong>Comentarios del jefe:</strong></p>
                        <p style='font-size: 14px; color: #64748b; font-style: italic;'>""{comments ?? "Sin comentarios adicionales"}""</p>
                        <hr style='border: 0; border-top: 1px solid #e2e8f0; margin: 30px 0;' />
                        <p style='font-size: 12px; color: #94a3b8; text-align: center;'>Este es un mensaje automático del Portal de Vacaciones MESA.</p>
                    </div>
                </div>";

            await _emailService.SendEmailAsync(new List<string> { employee.Email }, subject, body);
        }
    }
}