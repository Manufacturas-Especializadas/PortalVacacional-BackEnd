using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entities
{
    public class VacationRequestApproval
    {
        public int Id { get; set; }

        public int VacationRequestId { get; set; }

        public int ApproverId { get; set; }

        public int ApprovalLevel { get; set; }

        public int StatusId { get; set; }

        public string? Comments { get; set; }

        public DateTime? DecisionDate { get; set; }

        public VacationRequest VacationRequest { get; set; } = null!;

        public User Approver { get; set; } = null!;

        public Status Status { get; set; } = null!;
    }
}