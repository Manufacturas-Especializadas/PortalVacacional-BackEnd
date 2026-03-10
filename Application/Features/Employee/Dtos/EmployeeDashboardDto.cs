using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Employee.Dtos
{
    public class EmployeeDashboardDto
    {
        public int TotalDays { get; set; }

        public int UsedDays { get; set; }

        public int AvailableDays => TotalDays - UsedDays;

        public List<VacationRequestDto> History { get; set; }
    }
}