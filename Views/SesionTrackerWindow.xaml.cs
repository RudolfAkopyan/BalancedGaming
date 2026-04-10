using Balanced_Gaming.Data;
using Balanced_Gaming.Models;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Threading;

namespace Balanced_Gaming.Views
{
    public partial class SessionTrackerWindow : Window
    {
        private DateTime sessionStartTime;
        private DispatcherTimer timer;
        private bool isSessionActive = false;
        private int currentUserId = 1;

        public SessionTrackerWindow()
        {
            InitializeComponent();
            InitializeTimer();  
            LoadGames();
            LoadRecentSessions();

        }

        private void InitializeTimer() 
        {
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (isSessionActive)
            {
                TimeSpan elapsed = DateTime.Now - sessionStartTime;
                durationText.Text = $"{elapsed.Hours:D2}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}";
            }
        }

        private void LoadGames()
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    db.Database.EnsureCreated();

                    var games = db.games.ToList();

                    if (!games.Any())
                    {
                        var defaultGames = new List<Game>
                {
                    new Game { gameName = "Minecraft", genre = "Sandbox", processName = "", addedAt = DateTime.Now },
                    new Game { gameName = "Dota 2", genre = "MOBA", processName = "", addedAt = DateTime.Now },
                    new Game { gameName = "Counter-Strike 2", genre = "FPS", processName = "", addedAt = DateTime.Now },
                    new Game { gameName = "Valorant", genre = "FPS", processName = "", addedAt = DateTime.Now },
                    new Game { gameName = "Fortnite", genre = "Battle Royale", processName = "", addedAt = DateTime.Now }
                };

                        db.games.AddRange(defaultGames);
                        db.SaveChanges();

                        games = db.games.ToList();
                    }

                    gameComboBox.ItemsSource = games;
                    if (games.Any())
                        gameComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadRecentSessions()
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    var sessions = db.gameSessions
                        .Where(s => s.userId == currentUserId)
                        .OrderByDescending(s => s.startTime)
                        .Take(20)
                        .ToList();

                    var sessionDisplay = sessions.Select(s => new
                    {
                        GameName = db.games.FirstOrDefault(g => g.gameId == s.gameId)?.gameName ?? "Unknown",
                        s.startTime,
                        s.endTime,
                        StartTime = s.startTime,
                        EndTime = s.endTime,
                        DurationFormatted = s.endTime.HasValue
                        ? FormatDuration((s.endTime.Value - s.startTime).TotalMinutes)
                        : "In Progress"
                    }).ToList();                  
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading sessions: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string FormatDuration(double totalMinutes)
        {
            int hours = (int)(totalMinutes / 60);
            int minutes = (int)(totalMinutes % 60);

            if (hours > 0)
                return $"{hours}h {minutes}m";
            else
                return $"{minutes}m";
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (gameComboBox.SelectedItem == null)
            {
                MessageBox.Show("Please select a game first!", "No game selected!",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var selectedGame = (Game)gameComboBox.SelectedItem;

                // Show PRE-GAME mood assessment
                var moodWindow = new MoodAssessmentWindow(isPreGame: true);
                moodWindow.Owner = this;

                bool hasMoodData = moodWindow.ShowDialog() == true;

                // Start session
                sessionStartTime = DateTime.Now;
                isSessionActive = true;

                int newSessionId;

                using (var db = new AppDbContext())
                {
                    var session = new GameSession
                    {
                        userId = currentUserId,
                        gameId = selectedGame.gameId,
                        startTime = sessionStartTime,
                        endTime = null
                    };
                    db.gameSessions.Add(session);
                    db.SaveChanges();

                    newSessionId = session.sessionId;

                    // Save pre-game mood assessment if provided
                    if (hasMoodData)
                    {
                        var moodAssessment = new MoodAssessment
                        {
                            sessionId = newSessionId,
                            userId = currentUserId,
                            assessmentTime = DateTime.Now,
                            type = AssessmentType.BeforeGaming,
                            moodScore = moodWindow.MoodValue,
                            stressLevel = moodWindow.StressValue,
                            energyLevel = moodWindow.EnergyValue,
                            motivation = moodWindow.MotivationValue,
                            focusLevel = moodWindow.FocusValue
                        };
                        db.moodAssessments.Add(moodAssessment);
                        db.SaveChanges();
                    }
                }

                statusText.Text = $"Playing {selectedGame.gameName}";
                statusText.Foreground = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#27AE60"));
                startTimeText.Text = sessionStartTime.ToString("HH:mm");
                endTimeText.Text = "--:--";

                startButton.IsEnabled = false;
                stopButton.IsEnabled = true;
                gameComboBox.IsEnabled = false;

                timer.Start();
                var mainWindow = Application.Current.MainWindow as MainWindow;
                mainWindow?.StartReminderTimer(selectedGame.gameName);
                MessageBox.Show($"Session started for {selectedGame.gameName}!\nEnjoy your gaming!",
                   "Session started", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error starting session: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                isSessionActive = false;
            }
        }

        private void StartSessionWithoutMood(Game selectedGame)
        {
            sessionStartTime = DateTime.Now;
            isSessionActive = true;

            using (var db = new AppDbContext())
            {
                var session = new GameSession
                {
                    userId = currentUserId,
                    gameId = selectedGame.gameId,
                    startTime = sessionStartTime,
                    endTime = null
                };
                db.gameSessions.Add(session);
                db.SaveChanges();
            }

            statusText.Text = $"Playing {selectedGame.gameName}";
            statusText.Foreground = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#27AE60"));
            startTimeText.Text = sessionStartTime.ToString("HH:mm");
            endTimeText.Text = "--:--";

            startButton.IsEnabled = false;
            stopButton.IsEnabled = true;
            gameComboBox.IsEnabled = false;

            timer.Start();

            MessageBox.Show($"Session started for {selectedGame.gameName}!\nEnjoy your gaming!",
               "Session started", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isSessionActive)
                return;

            try
            {
                var selectedGame = (Game)gameComboBox.SelectedItem;
                DateTime endTime = DateTime.Now;
                endTimeText.Text = endTime.ToString("HH:mm");
                TimeSpan duration = endTime - sessionStartTime;

                // Show POST-GAME mood assessment
                var moodWindow = new MoodAssessmentWindow(isPreGame: false);
                moodWindow.Owner = this;

                bool hasMoodData = moodWindow.ShowDialog() == true;

                using (var db = new AppDbContext())
                {
                    var session = db.gameSessions
                        .Where(s => s.userId == currentUserId && s.endTime == null)
                        .OrderByDescending(s => s.startTime)
                        .FirstOrDefault();

                    if (session != null)
                    {
                        session.endTime = endTime;
                        db.SaveChanges();

                        // Save post-game mood assessment if provided
                        if (hasMoodData)
                        {
                            var moodAssessment = new MoodAssessment
                            {
                                sessionId = session.sessionId,
                                userId = currentUserId,
                                assessmentTime = DateTime.Now,
                                type = AssessmentType.AfterGaming,
                                moodScore = moodWindow.MoodValue,
                                stressLevel = moodWindow.StressValue,
                                energyLevel = moodWindow.EnergyValue,
                                satisfaction = moodWindow.SatisfactionValue,
                                wellbeingImpact = moodWindow.ImpactValue
                            };
                            db.moodAssessments.Add(moodAssessment);
                            db.SaveChanges();
                        }
                    }
                }

                timer.Stop();
                isSessionActive = false;
                var mainWindow = Application.Current.MainWindow as MainWindow;
                mainWindow?.StopReminderTimer();

                string durationStr = FormatDuration(duration.TotalMinutes);
                MessageBox.Show(
                    $"Session completed!\n\n" +
                    $"Game: {selectedGame.gameName}\n" +
                    $"Duration: {durationStr}\n" +
                    $"Started: {sessionStartTime.ToString("HH:mm")}\n" +
                    $"Ended: {endTime.ToString("HH:mm")}",
                    "Session summary",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                statusText.Text = "No active session";
                statusText.Foreground = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#95A5A6"));
                durationText.Text = "00:00:00";
                startTimeText.Text = "--:--";

                startButton.IsEnabled = true;
                stopButton.IsEnabled = false;
                gameComboBox.IsEnabled = true;

                LoadRecentSessions();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error stopping session: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void AddGameButton_Click(object sender, RoutedEventArgs e)
        {
            var addGameWindow = new AddGameWindow();
            addGameWindow.Owner = this;

            if (addGameWindow.ShowDialog() == true)
            {
                LoadGames(); // Refresh the game list
                MessageBox.Show("Game added successfully!", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }


        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadRecentSessions();
            MessageBox.Show("Sessions refreshed!", "Refresh",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)  // FIXED: was RoutedEvent
        {
            if (isSessionActive)
            {
                var result = MessageBox.Show(
                    "You have an active gaming session!\n\nDo you want to stop it before closing?",
                    "Active Session",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    StopButton_Click(sender, e);
                    this.Close();
                }
                else if (result == MessageBoxResult.No)
                {
                    this.Close();
                }
            }
            else
            {
                this.Close();
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)  // FIXED: was private
        {
            if (isSessionActive)
            {
                var result = MessageBox.Show(
                    "Active session will keep running in background.\nClose anyway?",
                    "Active Session Warning",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.No)
                {
                    e.Cancel = true;
                }
            }
            if (!isSessionActive)
            {
                var mainWindow = Application.Current.MainWindow as MainWindow;
                mainWindow?.StopReminderTimer();
            }
            base.OnClosing(e);
        }
    }
}