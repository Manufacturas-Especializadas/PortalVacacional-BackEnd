using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.GetEmployees
{
    public class EmployeeListDto
    {
        public int PayRollNumber { get; set; }

        public string FullName { get; set; }

        public string Department {  get; set; }

        public int YearsOfService { get; set; }

        public decimal TotalVacationDays { get; set; }
    }
}