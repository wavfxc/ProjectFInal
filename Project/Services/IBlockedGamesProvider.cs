using System.Collections.Generic;
using System.Threading.Tasks;
using Project.Models;

namespace Project.Services
{
    public interface IBlockedGamesProvider
    {
        Task<List<SteamGame>> GetBlockedInRussiaAsync();
    }
}