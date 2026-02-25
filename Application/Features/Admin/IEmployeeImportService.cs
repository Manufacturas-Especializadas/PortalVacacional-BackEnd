using Application.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Admin
{
    public interface IEmployeeImportService
    {
        Task<ImportEmployeesResultDto> ImportAsync(Stream fileStream, int importedByUserId);
    }
}