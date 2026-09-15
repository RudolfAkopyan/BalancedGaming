using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Balanced_Gaming.Models
{
    public class SteamGame
    {
        public int AppId { get; set; }
        public string Name { get; set; }
        public int PlaytimeForever { get; set; }
        public string IconUrl { get; set; }
    }
    public class SteamService
    {
        private readonly HttpClient _httpClient;
        private string _steamApiKey;
        private string _steamId;

        public SteamService()
        {
            _httpClient = new HttpClient();
        }
        public void SetCredentials(string apiKey, string steamId)
        {
            _steamApiKey = apiKey;
            _steamId = steamId;
        }
        public bool IsSteamInstalled()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam"))
                {
                    if (key != null)
                    {
                        string steamPath = key.GetValue("SteamPath")?.ToString();
                        return !string.IsNullOrEmpty(steamPath);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error steam is not installed on computer: {ex.Message}");
            }
            return false;
        }
        public string GetSteamPath()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam"))
                {
                    return key?.GetValue("SteamPath")?.ToString();
                }
            }
            catch
            {
                return null;
            }
        }
        public async Task<List<SteamGame>> GetOwnedGamesAsync()
        {
            if (string.IsNullOrEmpty(_steamApiKey) || string.IsNullOrEmpty(_steamId))
            {
                throw new Exception("Steam Api key and Steam Id must be set first");
            }
            try
            {
                string url = $"https://api.steampowered.com/IPlayerService/GetOwnedGames/v1/" +
                $"?key={_steamApiKey}&steamid={_steamId}&include_appinfo=1&format=json";

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                string jsonResponse = await response.Content.ReadAsStringAsync();
                var games = ParseGamesFromJson(jsonResponse);

                return games;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching Steam games: {ex.Message}");
                return new List<SteamGame>();
            }
        }
        public async Task<string> GetGameDetailsAsync(int appId)
        {
            try
            {
                string url = $"https://store.steampowered.com/api/appdetails?appids={appId}";
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception)
            {
                return null;
            }
        }
        private List<SteamGame> ParseGamesFromJson(string json)
        {
            var games = new List<SteamGame>();

            try
            {
                using (JsonDocument doc = JsonDocument.Parse(json))
                {
                    var root = doc.RootElement;
                    if (root.TryGetProperty("response", out var response) && response.TryGetProperty("games", out var gamesArray))
                    {
                        foreach (var game in gamesArray.EnumerateArray())
                        {
                            var steamGame = new SteamGame
                            {
                                AppId = game.GetProperty("appid").GetInt32(),
                                Name = game.GetProperty("name").GetString(),
                                PlaytimeForever = game.TryGetProperty("playtime_forever", out var playtime)
                                ? playtime.GetInt32() : 0
                            };
                            if (game.TryGetProperty("img_icon_url", out var iconHash))
                            {
                                string hash = iconHash.GetString();
                                if(!string.IsNullOrEmpty(hash))
                                {
                                    steamGame.IconUrl = $"https://media.steampowered.com/steamcommunity/public/images/apps/{steamGame.AppId}/{hash}.jpg";
                                }
                            }
                            games.Add(steamGame);
                        }
                    }
                }    
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing Steam games: { ex.Message}");
            }
            return games;
        }
    }
}
