using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
namespace Balanced_Gaming.Models
{
    public class Game
    {
        [Key]
        public int gameId { get; set; }
        public string gameName { get; set; }
        public string processName { get; set; }
        public string genre { get; set; }
        public DateTime addedAt { get; set; }
        public int? steamAppId { get; set; }
        public bool isTracked { get; set; } = true;
        public bool isManuallyAdded { get; set; } = false;

        //Nav property (foreign key) 
        public List<GameSession> gameSessions { get; set; } = new List<GameSession>();
    }
}
