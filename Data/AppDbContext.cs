using Balanced_Gaming.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Balanced_Gaming.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<User> users { get; set; }
        public DbSet<Game> games { get; set; }
        public DbSet<GameSession> gameSessions { get; set; }
        public DbSet<MoodAssessment> moodAssessments { get; set; }
        public DbSet<ReminderSettings> reminderSettings { get; set; }
        public DbSet<GameReminderException> gameReminderExceptions { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string dbPath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "BalancedGaming",
                "balancedfaming.db"
                );
        
            string folder = System.IO.Path.GetDirectoryName(dbPath);
            if (!System.IO.Directory.Exists(folder))
            {
                System.IO.Directory.CreateDirectory(folder);
            }

            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasMany(u => u.gameSessions)
                .WithOne(gs => gs.user)
                .HasForeignKey(gs => gs.userId)
                .OnDelete(DeleteBehavior.Cascade);
            
            modelBuilder.Entity<User>()
                .HasMany(u => u.moodAssessments)
                .WithOne(ma => ma.user)  
                .HasForeignKey(ma => ma.userId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Game>()
                .HasMany(g => g.gameSessions)
                .WithOne(gs => gs.game)
                .HasForeignKey(gs => gs.gameId)
                .OnDelete(DeleteBehavior.Cascade);        

            modelBuilder.Entity<GameSession>()
                .HasMany<MoodAssessment>()
                .WithOne(ma => ma.gameSession)
                .HasForeignKey(ma => ma.sessionId)
                .OnDelete(DeleteBehavior.SetNull);

        }
    }
}
