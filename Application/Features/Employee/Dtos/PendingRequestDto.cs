using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Employee.Dtos
{
    public class PendingRequestDto
    {
        public int RequestId { get; set; }

        public string EmployeeName { get; set; } = null!;

        public int PayrollNumber { get; set; }

        public string Department { get; set; } = null!;

        public string StartDate { get; set; } = null!;

        public string EndDate { get; set; } = null!;

        public int DaysRequested { get; set; }

        public string RequestedAt { get; set; } = null!;

        public string? Status { get; set; }
    }
}
