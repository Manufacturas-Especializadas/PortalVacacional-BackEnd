using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entities
{
    public class User
    {
        public int Id { get; set; }

        public int PayRollNumber { get; set; }
        
        public string FullName { get; set; }

        public string? Email { get; set; }

        public string PasswordHash { get; set; } = null!;

        public int RoleId { get; set; }

        public bool MustChangePassword { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        public Role Role { get; set; } = null!;

        public EmployeeProfile? EmployeeProfile { get; set; }

        public ICollection<VacationBalance> VacationBalances { get; set; } = new List<VacationBalance>();

        public ICollection<VacationRequest> VacationRequests { get; set; } = new List<VacationRequest>();
    }
}