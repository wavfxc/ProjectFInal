using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Project.Models;

namespace Project.Services
{
    public class BlockedGamesProvider : IBlockedGamesProvider
    {
        private static readonly (string AppId, string FallbackTitle)[] KnownTitles = new[]
        {
            ("990080", "Hogwarts Legacy"),
            ("1817070", "Marvel's Spider-Man Remastered"),
            ("1817190", "Marvel's Spider-Man: Miles Morales"),
            ("271590", "Grand Theft Auto V"),
            ("1551360", "Forza Horizon 5"),
            ("1245620", "Elden Ring"),
            ("1086940", "Baldur's Gate 3"),
            ("2050650", "Resident Evil 4"),
            ("1462040", "Hi-Fi Rush"),
            ("1888160", "Diablo IV"),
            ("2208920", "Lords of the Fallen"),
            ("1238840", "Battlefield 2042"),
            ("1938090", "Call of Duty: Modern Warfare III"),
            ("1172470", "Apex Legends"),
            ("1599340", "Lost Ark")
        };

        private readonly HttpClient _httpClient;

        public BlockedGamesProvider(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<SteamGame>> GetBlockedInRussiaAsync()
        {
            var result = new List<SteamGame>();

            foreach (var entry in KnownTitles)
            {
                var game = await CheckGameAsync(entry.AppId, entry.FallbackTitle);
                if (game != null)
                    result.Add(game);
            }

            return result;
        }

        private async Task<SteamGame> CheckGameAsync(string appId, string fallbackTitle)
        {
            string url = $"https://store.steampowered.com/api/appdetails?appids={appId}&cc=RU&l=russian";

            try
            {
                var response = await _httpClient.GetStringAsync(url);
                using var doc = JsonDocument.Parse(response);

                if (!doc.RootElement.TryGetProperty(appId, out var appRoot))
                    return BuildBlockedEntry(appId, fallbackTitle);

                bool success = appRoot.TryGetProperty("success", out var successProp) && successProp.GetBoolean();
                if (success)
                    return null;

                return BuildBlockedEntry(appId, fallbackTitle);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                return BuildBlockedEntry(appId, fallbackTitle);
            }
        }

        private static SteamGame BuildBlockedEntry(string appId, string fallbackTitle)
        {
            return new SteamGame
            {
                AppId = appId,
                Title = fallbackTitle,
                ImageUrl = $"https://cdn.cloudflare.steamstatic.com/steam/apps/{appId}/header.jpg",
                Price = "Недоступно",
                CurrentPrice = "Недоступно",
                Description = "Покупка недоступна для вашего региона.",
                StoreUrl = $"https://store.steampowered.com/app/{appId}/"
            };
        }
    }
}