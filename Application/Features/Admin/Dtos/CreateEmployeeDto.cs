using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Admin.Dtos
{
    public class CreateEmployeeDto
    {
        public int PayRollNumber { get; set; }

        public string FullName { get; set; }

        public string Department { get; set; }

        public string Email { get; set; }

        public int? ManagerId { get; set; }

        public int? RoleId { get; set; }

        public DateTime HireDate { get; set; }

        public List<VacationBalanceDto>? Balances { get; set; } = new();

    }
}