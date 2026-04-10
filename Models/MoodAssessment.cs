using System;
using System.ComponentModel.DataAnnotations;

namespace Balanced_Gaming.Models
{
    public class MoodAssessment
    {
        [Key]
        public int assessmentId { get; set; }
        public int? sessionId { get; set; }
        public int userId { get; set; }
        public DateTime assessmentTime { get; set; }
        public AssessmentType type { get; set; }

        // Core 3 questions (both pre and post)
        public int moodScore { get; set; }          // 1-5
        public int stressLevel { get; set; }        // 1-5
        public int energyLevel { get; set; }        // 1-5

        // Pre-game specific
        public string? motivation { get; set; }     // Dropdown value
        public int? focusLevel { get; set; }        // 1-5

        // Post-game specific
        public int? satisfaction { get; set; }      // 1-5
        public int? wellbeingImpact { get; set; }   // 1-5

        public string? notes { get; set; }

        // Navigation properties
        public User user { get; set; }
        public GameSession gameSession { get; set; }
    }

    public enum AssessmentType
    {
        BeforeGaming,
        AfterGaming,
        General
    }
}