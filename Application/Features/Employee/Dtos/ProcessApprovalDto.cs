using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Employee.Dtos
{
    public class ProcessApprovalDto
    {
        public int RequestId { get; set; }

        public bool Approved { get; set; }

        public string? Comments { get; set; }
    }
}
