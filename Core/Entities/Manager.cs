using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entities
{
    public class Manager
    {
        public int Id { get; set; }

        public string FullName { get; set; } = null!;

        public string Email { get; set; } = null!;

        public int PayRollNumber { get; set; }

        public int DepartmentId { get; set; }

        public int RoleId { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; }

        public Role Role { get; set; } = null!;

        public Department Department { get; set; } = null!;
    }
}