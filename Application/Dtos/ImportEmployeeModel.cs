using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dtos
{
    public class ImportEmployeeModel
    {
        public int PayrollNumber { get; set; }

        public string FullName { get; set; } = null!;

        public string Department { get; set; } = null!;

        public DateTime HireDate { get; set; }


        public int Vacation2024 { get; set; }

        public int Vacation2025 { get; set; }

        public int Vacation2026 { get; set; }
    }
}
