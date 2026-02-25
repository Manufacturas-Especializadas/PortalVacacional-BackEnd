using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entities
{
    public class VacationBalance
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public int Year { get; set; }

        public int AssignedDays { get; set; }

        public int UsedDays { get; set; }

        public DateTime CreatedAt { get; set; }

        public User User { get; set; } = null!;
    }
}