using Balanced_Gaming.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Balanced_Gaming
{

    public partial class SteamLibraryWindow : Window
    {
        private SteamService _steamService;

        public SteamLibraryWindow()
        {
            InitializeComponent();
            _steamService = new SteamService();

            string apiKey = "3471B871533DC2534115608029924EC3";
            string steamId = "76561198326739787";
            _steamService = new SteamService();
            _steamService.SetCredentials("3471B871533DC2534115608029924EC3", "76561198326739787");
            _steamService.SetCredentials(apiKey, steamId); 
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
                MessageBox.Show($"Loaded {games.Count} games from your Steam library!",
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading Steam games: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                LoadGamesButton.IsEnabled = true;
                LoadGamesButton.Content = "Reload Games";
            }
        }
    }
}
