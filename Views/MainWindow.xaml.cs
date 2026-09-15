using System.Drawing;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using Microsoft.EntityFrameworkCore;
using Balanced_Gaming.Data;
using Balanced_Gaming.Models;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using MessageBox = System.Windows.MessageBox;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using System.Windows.Threading;


namespace Balanced_Gaming.Views
{
    public partial class MainWindow : Window
    {
        private NotifyIcon notifyIcon;
        private SteamService _steamService;
        private AppSettings _settings;
        public MainWindow()
        {
            InitializeComponent();
            _steamService = new SteamService();
            _settings = AppSettings.Load();

            if (_settings.IsConfigured)
                _steamService.SetCredentials(_settings.SteamApiKey, _settings.SteamId);
        }
        private void SaveSteamSettings_Click(object sender, RoutedEventArgs e)
        {
            var key = steamApiKeyBox.Password.Trim();
            var id = steamIdBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(id))
            {
                steamSettingsStatus.Text = "Both fields are required";
                return;
            }

            _settings.SteamApiKey = key;
            _settings.SteamId = id;

            try
            {
                _settings.Save();
                _steamService.SetCredentials(key, id);
                steamSettingsStatus.Text = "Saved";
            }
            catch (Exception ex)
            {
                steamSettingsStatus.Text = "Save failed: " + ex.Message;
            }
        }

        private void OpenSessionTracker_Click(object sender, RoutedEventArgs e)
        {
            var sessionTracker = new SessionTrackerWindow();
            sessionTracker.Show();
        }

        private void InitializeDatabase()
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    db.Database.EnsureCreated();
                    if (!db.users.Any())
                    {
                        var user = new User
                        {
                            username = "Player1",
                            createdAt = DateTime.Now,
                        };
                        db.users.Add(user);
                        db.SaveChanges();

                        var defaultGames = new List<string> { "Dota 2", "Counter-Strike 2", "Minecraft", "Valorant", "Fortnite" };
                        foreach (var gameName in defaultGames)
                        {
                            {
                                if (!db.games.Any(g => g.gameName == gameName))
                                    db.games.Add(new Game { gameName = gameName, addedAt = DateTime.Now, processName = "", genre = "" });
                            }
                            db.SaveChanges();
                            MessageBox.Show("Welcome! Database created successfully", "BalancedGaming", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Database initialization failed: " + ex.Message,
                    "Error"
                );
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeDatabase();
            LoadUserInfo();
            LoadDashboard();
            LoadRemindersTab();

            // Create system tray icon
            notifyIcon = new NotifyIcon();
            notifyIcon.Text = "BalancedGaming";
            var iconStream = System.Windows.Application.GetResourceStream(
            new Uri("pack://application:,,,/Resources/BalancedGaming.ico")).Stream;
            notifyIcon.Icon = new Icon(iconStream, SystemInformation.SmallIconSize);
            notifyIcon.Visible = true;
            notifyIcon.DoubleClick += (s, args) => ShowMainWindow();

            var contextMenu = new System.Windows.Forms.ContextMenuStrip();
            contextMenu.Items.Add("Open Dashboard", null, (s, args) => ShowMainWindow());
            contextMenu.Items.Add("-");
            contextMenu.Items.Add("Exit", null, (s, args) => ExitApplication());
            notifyIcon.ContextMenuStrip = contextMenu;

        }
        private void LoadDashboard()
        {
            try
            {
                using (var context = new AppDbContext())
                {
                    var today = DateTime.Today;
                    var weekStart = today.AddDays(-(int)today.DayOfWeek);

                    var todaySessions = context.gameSessions
                        .Include(s => s.game)
                        .Where(s => s.startTime.Date == today && s.endTime != null)
                        .ToList();

                    var weekSessions = context.gameSessions
                        .Include(s => s.game)
                        .Where(s => s.startTime.Date >= weekStart && s.endTime != null)
                        .ToList();

                    // Today's time
                    int todayMinutes = todaySessions.Sum(s => (int)(s.endTime.Value - s.startTime).TotalMinutes);
                    dashTodayTimeText.Text = $"{todayMinutes / 60}h {todayMinutes % 60}m";
                    dashTodaySessionsText.Text = todaySessions.Count.ToString();
                    dashTodayGameText.Text = todaySessions.Any()
                        ? todaySessions.Last().game?.gameName ?? "Unknown"
                        : "No sessions yet";

                    // Progress bar (max 120 min)
                    double progressPercent = Math.Min((double)todayMinutes / 120.0, 1.0);
                    dashTodayLimitText.Text = $"{todayMinutes} / 120 min";
                    dashLimitProgressBar.Background = new System.Windows.Media.SolidColorBrush(
                        todayMinutes >= 120
                            ? System.Windows.Media.Color.FromRgb(231, 76, 60)
                            : System.Windows.Media.Color.FromRgb(52, 152, 219));

                    // Set progress bar width after layout
                    dashLimitProgressBar.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        double parentWidth = ((Border)dashLimitProgressBar.Parent).ActualWidth;
                        dashLimitProgressBar.Width = parentWidth * progressPercent;
                    }), System.Windows.Threading.DispatcherPriority.Loaded);

                    // Limit warning
                    if (todayMinutes >= 120)
                    {
                        dashLimitWarning.Visibility = Visibility.Visible;
                        dashLimitWarningText.Text = $"⚠️ You've played {todayMinutes / 60}h {todayMinutes % 60}m today. Consider taking a break!";
                    }
                    else
                    {
                        dashLimitWarning.Visibility = Visibility.Collapsed;
                    }

                    // This week
                    int weekMinutes = weekSessions.Sum(s => (int)(s.endTime.Value - s.startTime).TotalMinutes);
                    dashWeekTimeText.Text = $"{weekMinutes / 60}h {weekMinutes % 60}m";
                    dashWeekSessionsText.Text = $"{weekSessions.Count} sessions";

                    // Most played this week
                    var mostPlayed = weekSessions
                        .GroupBy(s => s.game?.gameName ?? "Unknown")
                        .OrderByDescending(g => g.Sum(s => (s.endTime.Value - s.startTime).TotalMinutes))
                        .FirstOrDefault();
                    dashMostPlayedText.Text = mostPlayed != null ? $"Most played: {mostPlayed.Key}" : "";

                    // Mood change (before vs after today)
                    var allMoods = context.moodAssessments.ToList();
                    var todaySessionIds = todaySessions.Select(s => s.sessionId).ToList();
                    var todayBefore = allMoods.Where(m => m.sessionId.HasValue && todaySessionIds.Contains(m.sessionId.Value) && m.type == AssessmentType.BeforeGaming).ToList();
                    var todayAfter = allMoods.Where(m => m.sessionId.HasValue && todaySessionIds.Contains(m.sessionId.Value) && m.type == AssessmentType.AfterGaming).ToList();

                    if (todayBefore.Any() && todayAfter.Any())
                    {
                        double avgBefore = todayBefore.Average(m => m.moodScore);
                        double avgAfter = todayAfter.Average(m => m.moodScore);
                        double delta = avgAfter - avgBefore;
                        dashMoodText.Text = delta >= 0 ? $"↑ +{delta:F1}" : $"↓ {delta:F1}";
                        dashMoodText.Foreground = new System.Windows.Media.SolidColorBrush(
                            delta >= 0
                                ? System.Windows.Media.Color.FromRgb(39, 174, 96)
                                : System.Windows.Media.Color.FromRgb(231, 76, 60));
                        dashMoodLabelText.Text = delta >= 0 ? "Gaming improved mood" : "Gaming lowered mood";
                    }
                    else
                    {
                        dashMoodText.Text = "N/A";
                        dashMoodText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(39, 174, 96));
                        dashMoodLabelText.Text = "No mood data today";
                    }

                    // Weekly chart - daily minutes
                    var dayLabels = new string[7];
                    var dayValues = new double[7];
                    for (int i = 0; i < 7; i++)
                    {
                        var day = weekStart.AddDays(i);
                        dayLabels[i] = day.ToString("ddd");
                        var daySessions = weekSessions.Where(s => s.startTime.Date == day).ToList();
                        dayValues[i] = daySessions.Sum(s => (s.endTime.Value - s.startTime).TotalMinutes);
                    }

                    dashWeeklyChart.Series = new ISeries[]
                    {
                new ColumnSeries<double>
                {
                    Values = dayValues,
                    Name = "Minutes",
                    Fill = new SolidColorPaint(SKColor.Parse("#3498DB"))
                }
                    };
                    dashWeeklyChart.XAxes = new Axis[] { new Axis { Labels = dayLabels } };
                    dashWeeklyChart.YAxes = new Axis[] { new Axis { Name = "Minutes" } };

                    // Mood before vs after chart (last 7 sessions)
                    var recentSessions = context.gameSessions
                        .Where(s => s.endTime != null)
                        .OrderByDescending(s => s.startTime)
                        .Take(7)
                        .ToList()
                        .OrderBy(s => s.startTime)
                        .ToList();

                    var beforeValues = new List<double>();
                    var afterValues = new List<double>();
                    var sessionLabels = new List<string>();

                    foreach (var s in recentSessions)
                    {
                        var before = allMoods.FirstOrDefault(m => m.sessionId == s.sessionId && m.type == AssessmentType.BeforeGaming);
                        var after = allMoods.FirstOrDefault(m => m.sessionId == s.sessionId && m.type == AssessmentType.AfterGaming);
                        if (before != null || after != null)
                        {
                            beforeValues.Add(before?.moodScore ?? 0);
                            afterValues.Add(after?.moodScore ?? 0);
                            sessionLabels.Add(s.startTime.ToString("MM/dd"));
                        }
                    }

                    dashMoodChart.Series = new ISeries[]
                    {
                new LineSeries<double>
                {
                    Values = beforeValues.ToArray(),
                    Name = "Before",
                    Stroke = new SolidColorPaint(SKColor.Parse("#3498DB")) { StrokeThickness = 3 },
                    GeometryStroke = new SolidColorPaint(SKColor.Parse("#3498DB")) { StrokeThickness = 3 },
                    GeometrySize = 8,
                    Fill = null
                },
                new LineSeries<double>
                {
                    Values = afterValues.ToArray(),
                    Name = "After",
                    Stroke = new SolidColorPaint(SKColor.Parse("#27AE60")) { StrokeThickness = 3 },
                    GeometryStroke = new SolidColorPaint(SKColor.Parse("#27AE60")) { StrokeThickness = 3 },
                    GeometrySize = 8,
                    Fill = null
                }
                    };
                    dashMoodChart.XAxes = new Axis[] { new Axis { Labels = sessionLabels.ToArray() } };
                    dashMoodChart.YAxes = new Axis[] { new Axis { MinLimit = 1, MaxLimit = 5, Name = "Mood" } };
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading dashboard: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void TestDatabase_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    var userCount = db.users.Count();
                    var gameCount = db.games.Count();
                    var sessionCount = db.gameSessions.Count();
                    var moodCount = db.moodAssessments.Count();

                    string message = "Database Status:\n" +
                     "✅ Users: " + userCount + "\n" +
                     "✅ Games: " + gameCount + "\n" +
                     "✅ Sessions: " + sessionCount + "\n" +
                     "✅ Mood Assessments: " + moodCount;

                    MessageBox.Show(message, "Database Test", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadUserInfo()
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    var user = db.users.FirstOrDefault();
                    if (user != null)
                    {
                        userWelcomeText.Text = "Welcome back, " + user.username + "!";
                        dbStatusText.Text = "Account created: " + user.createdAt.ToShortDateString();
                    }
                }
            }
            catch (Exception ex)
            {
                userWelcomeText.Text = "Error loading user";
                dbStatusText.Text = ex.Message;
            }
        }

        private void HideToTray_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }

        private void ShowMainWindow()
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
        }

        private void ExitApplication()
        {
            if (notifyIcon != null)
            {
                notifyIcon.Dispose();
            }
            System.Windows.Application.Current.Shutdown();
        }


        private void LoadGamingTimeChart(List<GameSession> sessions)
        {
            var dateMinutes = new Dictionary<DateTime, int>();

            foreach (var s in sessions)
            {
                DateTime date = s.startTime.Date;
                if (s.endTime != null)
                {
                    TimeSpan duration = s.endTime.Value - s.startTime;
                    int minutes = (int)duration.TotalMinutes;

                    if (dateMinutes.ContainsKey(date))
                    {
                        dateMinutes[date] = dateMinutes[date] + minutes;
                    }
                    else
                    {
                        dateMinutes[date] = minutes;
                    }
                }
            }

            var sortedDates = dateMinutes.Keys.OrderBy(d => d).ToList();
            var values = new double[sortedDates.Count];
            var labels = new string[sortedDates.Count];

            for (int i = 0; i < sortedDates.Count; i++)
            {
                values[i] = dateMinutes[sortedDates[i]];
                labels[i] = sortedDates[i].ToString("MM/dd");
            }
        }
       
        // Sessions Page
        private void ApplySessionsFilter_Click(object sender, RoutedEventArgs e)
        {
            LoadSessionsData();
        }

        private void ResetSessionsFilter_Click(object sender, RoutedEventArgs e)
        {
            sessionsStartDate.SelectedDate = null;
            sessionsEndDate.SelectedDate = null;
            LoadSessionsData();
        }
        private void ExportSessionsCSV_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Filter = "CSV files (*.csv)|*.csv",
                    FileName = "gaming_sessions_" + DateTime.Now.ToString("yyyy-MM-dd") + ".csv"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    var sessions = sessionsDataGrid.ItemsSource as List<SessionDisplayModel>;
                    if (sessions == null || sessions.Count == 0)
                    {
                        MessageBox.Show("No sessions to export.", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    var csv = new StringBuilder();
                    csv.AppendLine("Game,Date,Start Time,End Time,Duration,Mood Before,Mood After,Satisfaction");

                    foreach (var session in sessions)
                    {
                        csv.AppendLine("\"" + session.GameName + "\"," + session.Date + "," + session.StartTime + "," + session.EndTime + "," + session.Duration + "," + session.MoodBefore + "," + session.MoodAfter + "," + session.Satisfaction);
                    }

                    File.WriteAllText(saveDialog.FileName, csv.ToString());
                    MessageBox.Show("Exported " + sessions.Count + " sessions to " + saveDialog.FileName, "Export Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error exporting sessions: " + ex.Message, "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadSessionsData()
        {
            // Don't load if controls aren't initialized yet
            if (sessionsDataGrid == null || sessionsCountText == null || sessionsTotalTimeText == null)
            {
                return;
            }

            try
            {
                using (var context = new AppDbContext())
                {
                    var allSessions = context.gameSessions.Include(s => s.game).OrderByDescending(s => s.startTime).ToList();

                    // Apply filters
                    var filteredSessions = allSessions.Where(s =>
                    {
                        bool matchesStart = true;
                        bool matchesEnd = true;
                        bool matchesGame = true;

                        if (sessionsStartDate.SelectedDate.HasValue)
                        {
                            matchesStart = s.startTime >= sessionsStartDate.SelectedDate.Value;
                        }

                        if (sessionsEndDate.SelectedDate.HasValue)
                        {
                            var endDate = sessionsEndDate.SelectedDate.Value.AddDays(1);
                            matchesEnd = s.startTime < endDate;
                        }
            

                        return matchesStart && matchesEnd && matchesGame;
                    }).ToList();

                    var sessionIds = new List<int>();
                    foreach (var s in filteredSessions)
                    {
                        sessionIds.Add(s.sessionId);
                    }

                    var allMoods = context.moodAssessments.ToList();

                    var displayData = new List<SessionDisplayModel>();

                    foreach (var s in filteredSessions)
                    {
                        MoodAssessment beforeMood = null;
                        MoodAssessment afterMood = null;

                        foreach (var mood in allMoods)
                        {
                            if (mood.sessionId == s.sessionId)
                            {
                                if (mood.type == AssessmentType.BeforeGaming)
                                {
                                    beforeMood = mood;
                                }
                                else if (mood.type == AssessmentType.AfterGaming)
                                {
                                    afterMood = mood;
                                }
                            }
                        }

                        string gameName = "Unknown";
                        if (s.game != null)
                        {
                            gameName = s.game.gameName;
                        }

                        string endTimeStr = "N/A";
                        string durationStr = "0m";

                        if (s.endTime != null)
                        {
                            endTimeStr = s.endTime.Value.ToString("HH:mm");
                            TimeSpan dur = s.endTime.Value - s.startTime;
                            durationStr = ((int)dur.TotalMinutes) + "m";
                        }

                        string moodBeforeStr = "N/A";
                        if (beforeMood != null)
                        {
                            moodBeforeStr = beforeMood.moodScore.ToString();
                        }

                        string moodAfterStr = "N/A";
                        if (afterMood != null)
                        {
                            moodAfterStr = afterMood.moodScore.ToString();
                        }

                        string satisfactionStr = "N/A";
                        if (afterMood != null && afterMood.satisfaction != null)
                        {
                            int satValue = afterMood.satisfaction.Value;
                            satisfactionStr = satValue.ToString();
                        }

                        displayData.Add(new SessionDisplayModel
                        {
                            GameName = gameName,
                            Date = s.startTime.ToString("yyyy-MM-dd"),
                            StartTime = s.startTime.ToString("HH:mm"),
                            EndTime = endTimeStr,
                            Duration = durationStr,
                            MoodBefore = moodBeforeStr,
                            MoodAfter = moodAfterStr,
                            Satisfaction = satisfactionStr
                        });
                    }

                    sessionsDataGrid.ItemsSource = displayData;
                    sessionsCountText.Text = "Showing " + displayData.Count + " sessions";

                    int totalMinutes = 0;
                    foreach (var s in filteredSessions)
                    {
                        if (s.endTime != null)
                        {
                            TimeSpan dur = s.endTime.Value - s.startTime;
                            totalMinutes = totalMinutes + (int)dur.TotalMinutes;
                        }
                    }

                    int hours = totalMinutes / 60;
                    int mins = totalMinutes % 60;
                    sessionsTotalTimeText.Text = "Total: " + hours + "h " + mins + "m";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading sessions: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
   

        private async void LoadGamesButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadGamesButton.IsEnabled = false;
                LoadGamesButton.Content = "Loading...";
                var games = await _steamService.GetOwnedGamesAsync();
                var gamesWithHours = games
                    .OrderByDescending(g => g.PlaytimeForever)
                    .Select(g => new
                    {
                        g.Name,
                        g.AppId,
                        PlaytimeHours = $"{g.PlaytimeForever / 60.0:F1} hrs"
                    })
                    .ToList();
                GamesListView.ItemsSource = gamesWithHours;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load Steam games: " + ex.Message, "Steam", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                LoadGamesButton.IsEnabled = true;
                LoadGamesButton.Content = "Reload Games";
            }
        }
        private async void ImportSteamGames_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var btn = sender as System.Windows.Controls.Button;
                btn.IsEnabled = false;
                btn.Content = "Importing...";

                var games = await _steamService.GetOwnedGamesAsync();
                int imported = 0;
                using (var db = new AppDbContext())
                {
                    foreach (var game in games)
                    {
                        if (!db.games.Any(g => g.gameName == game.Name))
                        {
                            db.games.Add(new Game { gameName = game.Name, addedAt = DateTime.Now, processName = "", genre = "", steamAppId = game.AppId }); imported++;
                        }
                    }
                    db.SaveChanges();
                }    
            
               
            }
            catch (Exception ex)
            {
                string details = ex.Message;
                if (ex.InnerException != null)
                    details += "\n\nInner: " + ex.InnerException.Message;
                if (ex.InnerException?.InnerException != null)
                    details += "\n\nInner2: " + ex.InnerException.InnerException.Message;
                MessageBox.Show(details, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                var btn = sender as System.Windows.Controls.Button;
                btn.IsEnabled = true;
                btn.Content = "Import to My Games";
            }
        }
        private void GamesListView_DoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (GamesListView.SelectedItem == null) return;

            dynamic selected = GamesListView.SelectedItem;
            string gameName = selected.Name;
            int appId = selected.AppId;

            using (var db = new AppDbContext())
            {
                var game = db.games.FirstOrDefault(g => g.gameName == gameName);
                var popup = new GameInfoWindow(gameName, game?.steamAppId, _steamService);
                popup.ShowDialog();
            }
        }


        private void LoadRemindersTab()
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    // Load or create settings
                    var settings = db.reminderSettings.FirstOrDefault();
                    if (settings == null)
                    {
                        settings = new ReminderSettings { IsEnabled = true, IntervalMinutes = 30, SnoozeDurationMinutes = 5 };
                        db.reminderSettings.Add(settings);
                        db.SaveChanges();
                    }

                    reminderEnabledToggle.IsChecked = settings.IsEnabled;
                    customIntervalBox.Text = settings.IntervalMinutes.ToString();
                    currentIntervalText.Text = $"Reminder every {settings.IntervalMinutes} minutes";
                    UpdateIntervalButtonStyles(settings.IntervalMinutes);

                    // Load games into picker
                    exceptionGamePicker.Items.Clear();
                    var games = db.games.OrderBy(g => g.gameName).ToList();
                    foreach (var g in games)
                    {
                        exceptionGamePicker.Items.Add(new ComboBoxItem
                        {
                            Content = g.gameName,
                            Tag = g.gameId,
                            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(230, 237, 243))
                        });
                    }
                    if (exceptionGamePicker.Items.Count > 0)
                        exceptionGamePicker.SelectedIndex = 0;
                    // Load existing exceptions
                    LoadExceptionsList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading reminders: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadExceptionsList()
        {
            using (var db = new AppDbContext())
            {
                var exceptions = db.gameReminderExceptions
                    .Include(e => e.Game)
                    .ToList()
                    .Select(e => new { GameId = e.GameId, GameName = e.Game?.gameName ?? "Unknown" })
                    .ToList();

                exceptionsListBox.ItemsSource = exceptions;
                noExceptionsText.Visibility = exceptions.Any() ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        private void ReminderSettings_Changed(object sender, RoutedEventArgs e)
        {
            SaveReminderSettings();
        }

        private void IntervalPreset_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as System.Windows.Controls.Button;
            if (btn == null) return;
            int minutes = int.Parse(btn.Tag.ToString());
            customIntervalBox.Text = minutes.ToString();
            SaveReminderSettings();
            UpdateIntervalButtonStyles(minutes);
        }

        private void CustomInterval_Changed(object sender, TextChangedEventArgs e)
        {
            if (int.TryParse(customIntervalBox.Text, out int minutes) && minutes > 0)
            {
                SaveReminderSettings();
                UpdateIntervalButtonStyles(minutes);
            }
        }

        private void UpdateIntervalButtonStyles(int selectedMinutes)
        {
            var presets = new[] { (interval10Btn, 10), (interval30Btn, 30), (interval60Btn, 60), (interval90Btn, 90) };
            foreach (var (btn, value) in presets)
            {
                if (selectedMinutes == value)
                {
                    btn.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(26, 46, 31));
                    btn.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(61, 220, 132));
                    btn.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(45, 90, 58));
                }
                else
                {
                    btn.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(33, 38, 45));
                    btn.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 122, 141));
                    btn.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(48, 54, 61));
                }
            }

            if (currentIntervalText != null)
                currentIntervalText.Text = $"Reminder every {selectedMinutes} minutes";
        }

        private void SaveReminderSettings()
        {
            try
            {
                // Guard against being called before UI is ready
                if (customIntervalBox == null || reminderEnabledToggle == null) return;
                if (!int.TryParse(customIntervalBox.Text, out int minutes) || minutes <= 0) return;

                using (var db = new AppDbContext())
                {
                    var settings = db.reminderSettings.FirstOrDefault();
                    if (settings == null)
                    {
                        settings = new ReminderSettings();
                        db.reminderSettings.Add(settings);
                    }
                    settings.IsEnabled = reminderEnabledToggle.IsChecked == true;
                    settings.IntervalMinutes = minutes;
                    settings.SnoozeDurationMinutes = 5;
                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving settings: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddGameException_Click(object sender, RoutedEventArgs e)
        {
            var selected = exceptionGamePicker.SelectedItem as ComboBoxItem;
            if (selected == null)
            {
                MessageBox.Show("Please select a game first.", "BalancedGaming", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            int gameId = (int)selected.Tag;

            using (var db = new AppDbContext())
            {
                if (!db.gameReminderExceptions.Any(x => x.GameId == gameId))
                {
                    db.gameReminderExceptions.Add(new GameReminderException { GameId = gameId });
                    db.SaveChanges();
                }
            }

            LoadExceptionsList();
        }
        private DispatcherTimer _reminderTimer;
        private DateTime _sessionStartTime;
        private string _currentGameName = "";

        public void StartReminderTimer(string gameName)
        {
            _currentGameName = gameName;
            _sessionStartTime = DateTime.Now;


            try
            {
                using (var db = new AppDbContext())
                {
                    var settings = db.reminderSettings.FirstOrDefault();



                    if (settings == null || !settings.IsEnabled) return;

                    var gameObj = db.games.FirstOrDefault(g => g.gameName == gameName);
                    bool isException = gameObj != null && db.gameReminderExceptions.Any(e => e.GameId == gameObj.gameId);


                    if (isException) return;

                    _reminderTimer?.Stop();
                    _reminderTimer = new DispatcherTimer();
                    _reminderTimer.Interval = TimeSpan.FromMinutes(settings.IntervalMinutes);
                    _reminderTimer.Tick += ReminderTimer_Tick;
                    _reminderTimer.Start();


                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error starting reminder: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void StopReminderTimer()
        {
            _reminderTimer?.Stop();
            _reminderTimer = null;
        }

        private void ReminderTimer_Tick(object sender, EventArgs e)
        {
            _reminderTimer?.Stop();

            this.Dispatcher.Invoke(() =>
            {
                int snoozeMinutes = 5;
                try
                {
                    using (var db = new AppDbContext())
                    {
                        var settings = db.reminderSettings.FirstOrDefault();
                        snoozeMinutes = settings?.SnoozeDurationMinutes ?? 5;
                    }
                }
                catch { }

                var result = MessageBox.Show(
                    $"You've been playing {_currentGameName} for a while.\nStep away and rest your eyes!\n\n" +
                    $"Yes = Snooze {snoozeMinutes} min\n" +
                    $"No = Stop Session\n" +
                    $"Cancel = Dismiss",
                    "⏰ Break Reminder — BalancedGaming",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Information);

                if (result == MessageBoxResult.Yes)
                {
                    _reminderTimer = new DispatcherTimer();
                    _reminderTimer.Interval = TimeSpan.FromMinutes(snoozeMinutes);
                    _reminderTimer.Tick += ReminderTimer_Tick;
                    _reminderTimer.Start();
                }
                else if (result == MessageBoxResult.No)
                {
                    foreach (System.Windows.Window w in System.Windows.Application.Current.Windows)
                    {
                        if (w is SessionTrackerWindow tracker)
                        {
                            tracker.Close();
                            break;
                        }
                    }
                    StopReminderTimer();
                }
            });
        }
        private void RemoveGameException_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as System.Windows.Controls.Button;
            if (btn == null) return;
            int gameId = (int)btn.Tag;

            using (var db = new AppDbContext())
            {
                var ex = db.gameReminderExceptions.FirstOrDefault(x => x.GameId == gameId);
                if (ex != null)
                {
                    db.gameReminderExceptions.Remove(ex);
                    db.SaveChanges();
                }
            }

            LoadExceptionsList();
        }
            
        public class SessionDisplayModel
        {
            public string GameName { get; set; }
            public string Date { get; set; }
            public string StartTime { get; set; }
            public string EndTime { get; set; }
            public string Duration { get; set; }
            public string MoodBefore { get; set; }
            public string MoodAfter { get; set; }
            public string Satisfaction { get; set; }
        }
    }
}