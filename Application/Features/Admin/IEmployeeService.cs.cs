using Application.Features.Admin.Dtos;
using Application.Features.Employee.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Admin
{
    public interface IEmployeeService
    {
        Task<EmployeeDashboardDto> GetDashboardDataAsync(int userId);

        Task<int> CreateEmployeeAsync(CreateEmployeeDto dto);

        Task<bool> UpdateEmployeeAsync(UpdateEmployeeDto dto);

        Task<bool> DeleteEmployeeAsync(int id);

        Task<bool> ReactivateEmployeeAsync(int id);
    }
}