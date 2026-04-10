using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Balanced_Gaming.Models
{
    public class GameSession
    {
        [Key]
        public int sessionId {  get; set; }
        public int userId { get; set; }
        public int gameId { get; set; }
        public DateTime startTime {  get; set; }
        public DateTime? endTime { get; set; }
        
        public TimeSpan? Duration
        {
            get
            {
                if (endTime.HasValue)
                    return endTime.Value - startTime;
                else 
                    return DateTime.Now - startTime;
            }
        }
        public int breakRemindersShown { get; set; } = 0;
        public int breakRemindersDismissed { get; set; } = 0;
        public int breakTaken {  get; set; } = 0;

        public User user { get; set; }
        public Game game { get; set; }
    }
}
