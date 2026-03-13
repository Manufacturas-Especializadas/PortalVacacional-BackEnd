using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Managers.Dtos
{
    public class MangersListDto
    {
        public int Id { get; set; }

        public int PayRollNumber { get; set; }

        public string FullName { get; set; }

        public string Department { get; set; }

        public int RoleId { get; set; }

        public DateTime HireDate { get; set; }

        public int YearsOfService { get; set; }

        public decimal TotalVacationDays { get; set; }

        public bool IsActive { get; set; }
    }
}