using Application.Features.Employee.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Employee
{
    public interface IVacationService
    {
        Task<List<PendingRequestDto>> GetPendingRequestsForManagerAsync(int managerPayroll);

        Task<bool> ProcessApprovalAsync(int managerPayroll, ProcessApprovalDto dto);
    }
}