using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Balanced_Gaming.Models
{
    public class ReminderSettings
    {
        [Key]
        public int Id { get; set; }
        public bool IsEnabled { get; set; }
        public int IntervalMinutes { get; set; } = 30;
        public int SnoozeDurationMinutes { get; set; } = 5;
    }
}
