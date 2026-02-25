using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entities
{
    public class ImportLog
    {
        public int Id { get; set; }

        public string FileName { get; set; } = null!;

        public int ImportedBy { get; set; }

        public DateTime ImportedAt { get; set; }

        public int RecordsProcessed { get; set; }

        public int RecordsInserted { get; set; }

        public int RecordsUpdated { get; set; }


        public User User { get; set; } = null!;
    }
}