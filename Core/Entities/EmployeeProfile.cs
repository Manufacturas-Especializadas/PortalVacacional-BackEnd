using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entities
{
    public class EmployeeProfile
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public int DepartmentId { get; set; }

        public DateTime HireDate { get; set; }

        public int? ManagerId { get; set; }

        public User User { get; set; } = null!;

        public Department Department { get; set; } = null!;

        public User? Manager { get; set; }
    }
}