using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Project.Models;

namespace Project.Services
{
    public interface ISteamParserService
    {
        Task<List<SteamGame>> GetDiscountsAsync(string countryCode, CancellationToken cancellationToken);
        Task<List<SteamGame>> SearchGamesAsync(string query, string countryCode, CancellationToken cancellationToken);
        Task<SteamGame> GetGameDetailsAsync(string appId, string countryCode);
        Task<bool> IsRegionAvailableAsync(string countryCode);
    }
}