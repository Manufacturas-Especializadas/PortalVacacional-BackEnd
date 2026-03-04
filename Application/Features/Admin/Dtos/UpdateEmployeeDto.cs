using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Admin.Dtos
{
    public class UpdateEmployeeDto
    {
        public int Id { get; set; }

        public int? PayRollNumber { get; set; }

        public string? FullName { get; set; }

        public string? Department { get; set; }

        public DateTime? HireDate { get; set; }

        public bool? IsActive { get; set; }

        public List<VacationBalanceDto>? Balances { get; set; }
    }
}