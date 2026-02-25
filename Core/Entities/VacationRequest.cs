using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entities
{
    public class VacationRequest
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public int RequestedDays { get; set; }

        public int StatusId { get; set; }

        public DateTime CreatedAt { get; set; }


        public User User { get; set; } = null!;

        public Status Status { get; set; } = null!;

        public ICollection<VacationRequestApproval> Approvals { get; set; }
            = new List<VacationRequestApproval>();
    }
}