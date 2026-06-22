using HtmlAgilityPack;
using Project.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Xml;

namespace Project.Services
{
    public class SteamParserService : ISteamParserService
    {
        private readonly HttpClient _httpClient;

        public SteamParserService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<SteamGame>> GetDiscountsAsync(string countryCode, CancellationToken cancellationToken)
        {
            string url = $"https://store.steampowered.com/search/?specials=1&cc={countryCode}";
            return await ParseSearchPageAsync(url, countryCode, cancellationToken);
        }

        public async Task<List<SteamGame>> SearchGamesAsync(string query, string countryCode, CancellationToken cancellationToken)
        {
            string encoded = HttpUtility.UrlEncode(query);
            string url = $"https://store.steampowered.com/search/?term={encoded}&cc={countryCode}";
            return await ParseSearchPageAsync(url, countryCode, cancellationToken);
        }

        public async Task<bool> IsRegionAvailableAsync(string countryCode)
        {
            try
            {
                string url = $"https://store.steampowered.com/search/?specials=1&cc={countryCode}";
                var response = await _httpClient.GetAsync(url);

                if (response.StatusCode == HttpStatusCode.Forbidden)
                    return false;

                if (!response.IsSuccessStatusCode)
                    return false;

                var html = await response.Content.ReadAsStringAsync();
                return html.Contains("search_result_row") || html.Contains("search_results");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                return false;
            }
        }

        public async Task<SteamGame> GetGameDetailsAsync(string appId, string countryCode)
        {
            var game = new SteamGame { AppId = appId };
            string url = $"https://store.steampowered.com/api/appdetails?appids={appId}&cc={countryCode}&l=russian";

            try
            {
                var response = await _httpClient.GetStringAsync(url);
                using var doc = JsonDocument.Parse(response);

                if (!doc.RootElement.TryGetProperty(appId, out var appRoot)) return game;
                if (!appRoot.TryGetProperty("success", out var success) || !success.GetBoolean())
                {
                    game.IsBlocked = true;
                    game.Description = "Покупка недоступна для вашего региона.";
                    return game;
                }

                var data = appRoot.GetProperty("data");

                if (data.TryGetProperty("name", out var nameProp))
                    game.Title = nameProp.GetString();

                if (data.TryGetProperty("short_description", out var descProp))
                    game.Description = descProp.GetString();

                if (data.TryGetProperty("header_image", out var headerProp))
                    game.ImageUrl = headerProp.GetString();

                if (data.TryGetProperty("price_overview", out var priceProp))
                {
                    if (priceProp.TryGetProperty("final_formatted", out var finalFmt))
                        game.CurrentPrice = finalFmt.GetString();
                    if (priceProp.TryGetProperty("initial_formatted", out var initialFmt))
                    {
                        string initialText = initialFmt.GetString();
                        game.OriginalPrice = string.IsNullOrEmpty(initialText) ? game.CurrentPrice : initialText;
                    }
                    if (priceProp.TryGetProperty("discount_percent", out var discProp))
                        game.DiscountValue = discProp.GetInt32();
                    if (priceProp.TryGetProperty("final", out var finalRaw))
                        game.PriceValue = finalRaw.GetInt32() / 100.0;
                }
                else if (data.TryGetProperty("is_free", out var freeProp) && freeProp.GetBoolean())
                {
                    game.CurrentPrice = "Бесплатно";
                    game.OriginalPrice = "Бесплатно";
                }

                game.MetacriticScore = data.TryGetProperty("metacritic", out var metaProp) &&
                                        metaProp.TryGetProperty("score", out var scoreProp)
                    ? scoreProp.GetInt32().ToString()
                    : "—";

                if (data.TryGetProperty("genres", out var genresProp))
                {
                    foreach (var genre in genresProp.EnumerateArray())
                    {
                        if (genre.TryGetProperty("description", out var gDesc))
                            game.Genres.Add(gDesc.GetString());
                    }
                }

                if (data.TryGetProperty("screenshots", out var scrProp))
                {
                    foreach (var scr in scrProp.EnumerateArray())
                    {
                        if (scr.TryGetProperty("path_full", out var pathProp))
                            game.Screenshots.Add(pathProp.GetString());
                    }
                }

                if (data.TryGetProperty("movies", out var moviesProp) && moviesProp.GetArrayLength() > 0)
                {
                    var firstMovie = moviesProp[0];
                    if (firstMovie.TryGetProperty("mp4", out var mp4Prop) && mp4Prop.TryGetProperty("max", out var maxProp))
                        game.TrailerUrl = maxProp.GetString();
                    else if (firstMovie.TryGetProperty("webm", out var webmProp) && webmProp.TryGetProperty("max", out var webmMaxProp))
                        game.TrailerUrl = webmMaxProp.GetString();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }

            return game;
        }

        private async Task<List<SteamGame>> ParseSearchPageAsync(string url, string countryCode, CancellationToken cancellationToken)
        {
            var games = new List<SteamGame>();

            try
            {
                var html = await _httpClient.GetStringAsync(url, cancellationToken);
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                var nodes = doc.DocumentNode.SelectNodes("//a[contains(@class, 'search_result_row')]");
                if (nodes == null) return games;

                var culture = GetCultureForRegion(countryCode);

                foreach (var node in nodes)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var titleNode = node.SelectSingleNode(".//span[@class='title']");
                    if (titleNode == null) continue;

                    var imgNode = node.SelectSingleNode(".//div[contains(@class, 'search_capsule')]/img");
                    var discountNode = node.SelectSingleNode(".//div[contains(@class, 'discount_pct')]");
                    var originalPriceNode = node.SelectSingleNode(".//div[contains(@class, 'discount_original_price')]");
                    var finalPriceNode = node.SelectSingleNode(".//div[contains(@class, 'discount_final_price')]");
                    var storeUrl = node.GetAttributeValue("href", "");

                    string title = titleNode.InnerText.Trim();
                    string discountText = discountNode?.InnerText.Trim() ?? "";
                    string currentPriceText = finalPriceNode?.InnerText.Trim() ?? "";

                    int.TryParse(discountText.Replace("-", "").Replace("%", ""), out int discountVal);
                    double priceVal = ParsePrice(currentPriceText, culture);

                    string appId = ExtractAppId(storeUrl);

                    games.Add(new SteamGame
                    {
                        AppId = appId,
                        Title = title,
                        ImageUrl = imgNode?.GetAttributeValue("src", ""),
                        DiscountPercent = string.IsNullOrEmpty(discountText) ? "0%" : discountText,
                        OriginalPrice = originalPriceNode?.InnerText.Trim() ?? currentPriceText,
                        CurrentPrice = currentPriceText,
                        PriceValue = priceVal,
                        DiscountValue = discountVal,
                        StoreUrl = storeUrl
                    });
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }

            return games;
        }

        private static string ExtractAppId(string storeUrl)
        {
            if (string.IsNullOrEmpty(storeUrl)) return string.Empty;
            var match = Regex.Match(storeUrl, @"/app/(\d+)/");
            return match.Success ? match.Groups[1].Value : string.Empty;
        }

        private static CultureInfo GetCultureForRegion(string countryCode)
        {
            return countryCode switch
            {
                "US" => new CultureInfo("en-US"),
                _ => new CultureInfo("ru-RU")
            };
        }

        private static double ParsePrice(string rawText, CultureInfo culture)
        {
            if (string.IsNullOrWhiteSpace(rawText)) return 0;

            string cleaned = new string(rawText.Where(c => char.IsDigit(c) || c == ',' || c == '.').ToArray());
            if (string.IsNullOrEmpty(cleaned)) return 0;

            if (double.TryParse(cleaned, NumberStyles.Any, culture, out double result))
                return result;

            string normalized = cleaned.Replace(".", "").Replace(",", ".");
            double.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
            return result;
        }
    }
}