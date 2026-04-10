using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Balanced_Gaming.Models
{
    public class User  
    {
        [Key]
        public int userId { get; set; }
        public string username { get; set; }
        public DateTime createdAt { get; set; }
        public int defaultBreakRemindersMinutes { get; set; } = 60;
        public bool breakRemindersEnabled { get; set; } = true;
        public bool moodTrackingEnabled { get; set; } = true;
        public List<GameSession> gameSessions { get; set; } = new List<GameSession>();
        public List<MoodAssessment> moodAssessments { get; set; } = new List<MoodAssessment>();
    }
}
