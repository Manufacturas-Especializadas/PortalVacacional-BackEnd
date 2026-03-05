using Application.Features.Admin.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Admin
{
    public interface IEmployeeService
    {
        Task<int> CreateEmployeeAsync(CreateEmployeeDto dto);

        Task<bool> UpdateEmployeeAsync(UpdateEmployeeDto dto);

        Task<bool> DeleteEmployeeAsync(int payrollNumber);
    }
}