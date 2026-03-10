using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Employee.Dtos
{
    public class VacationRequestDto
    {
        public int Id { get; set; }

        public string StartDate { get; set; }

        public string EndDate { get; set; }

        public int Days { get; set; }

        public string Status { get; set; }
    }
}