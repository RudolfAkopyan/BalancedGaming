using Balanced_Gaming.Models;
using System.Text.Json;
using System.Windows;

namespace Balanced_Gaming
{
    public partial class GameInfoWindow : Window
    {
        public GameInfoWindow(string gameName, int? steamAppId, SteamService steamService)
        {
            InitializeComponent();
            gameTitleText.Text = gameName;

            if (steamAppId == null)
            {
                developerText.Text = "N/A";
                genreText.Text = "N/A";
                releaseDateText.Text = "N/A";
                descriptionText.Text = "No Steam data available for this game.";
            }
            else
            {
                LoadDetailsAsync(steamAppId.Value, steamService);
            }
        }

        private async void LoadDetailsAsync(int appId, SteamService steamService)
        {
            developerText.Text = "Loading...";
            genreText.Text = "Loading...";
            releaseDateText.Text = "Loading...";
            descriptionText.Text = "Loading...";

            try
            {
                string json = await steamService.GetGameDetailsAsync(appId);
                if (json == null) throw new Exception("No response from Steam");

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                var appData = root.GetProperty(appId.ToString()).GetProperty("data");

                developerText.Text = appData.TryGetProperty("developers", out var devs) && devs.GetArrayLength() > 0
                    ? devs[0].GetString() : "Unknown";

                genreText.Text = appData.TryGetProperty("genres", out var genres) && genres.GetArrayLength() > 0
                    ? string.Join(", ", genres.EnumerateArray().Select(g => g.GetProperty("description").GetString())) : "Unknown";

                releaseDateText.Text = appData.TryGetProperty("release_date", out var release)
                    ? release.GetProperty("date").GetString() : "Unknown";

                descriptionText.Text = appData.TryGetProperty("short_description", out var desc)
                    ? desc.GetString() : "No description available.";
            }
            catch
            {
                developerText.Text = "Could not load";
                genreText.Text = "Could not load";
                releaseDateText.Text = "Could not load";
                descriptionText.Text = "Could not fetch details from Steam.";
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}