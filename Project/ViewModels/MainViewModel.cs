using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Project.Models;
using Project.Services;

namespace Project.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly ISteamParserService _parserService;
        private readonly IBlockedGamesProvider _blockedGamesProvider;

        private List<SteamGame> _allGames = new List<SteamGame>();
        private CancellationTokenSource _loadCts;
        private CancellationTokenSource _searchCts;

        public ObservableCollection<SteamGame> Games { get; } = new ObservableCollection<SteamGame>();
        public ObservableCollection<SteamGame> BlockedGames { get; } = new ObservableCollection<SteamGame>();

        public ObservableCollection<string> Regions { get; } = new ObservableCollection<string> { "Россия", "США", "Украина" };
        public ObservableCollection<string> SortOptions { get; } = new ObservableCollection<string> { "Популярность", "Цена: по возрастанию", "Цена: по убыванию" };

        private string _selectedRegion = "Россия";
        public string SelectedRegion
        {
            get => _selectedRegion;
            set
            {
                if (_selectedRegion == value) return;
                _selectedRegion = value;
                OnPropertyChanged();
                _ = LoadGamesAsync();
            }
        }

        private string _selectedSort = "Популярность";
        public string SelectedSort
        {
            get => _selectedSort;
            set
            {
                _selectedSort = value;
                OnPropertyChanged();
                ApplySort();
            }
        }

        private string _searchQuery = string.Empty;
        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                _searchQuery = value;
                OnPropertyChanged();
                _ = OnSearchQueryChangedAsync(value);
            }
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }

        private bool _isRegionAvailable = true;
        public bool IsRegionAvailable
        {
            get => _isRegionAvailable;
            set { _isRegionAvailable = value; OnPropertyChanged(); }
        }

        private bool _isSearchMode;
        public bool IsSearchMode
        {
            get => _isSearchMode;
            set { _isSearchMode = value; OnPropertyChanged(); }
        }

        private bool _hasBlockedGames;
        public bool HasBlockedGames
        {
            get => _hasBlockedGames;
            set { _hasBlockedGames = value; OnPropertyChanged(); }
        }

        private bool _isBlockedSectionExpanded;
        public bool IsBlockedSectionExpanded
        {
            get => _isBlockedSectionExpanded;
            set
            {
                _isBlockedSectionExpanded = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(BlockedSectionHeaderText));
            }
        }

        public string BlockedSectionHeaderText =>
            (IsBlockedSectionExpanded ? "▲ Скрыть" : "▼ Показать") +
            $" заблокированные в России игры ({BlockedGames.Count})";

        public ICommand RefreshCommand { get; }
        public ICommand ClearSearchCommand { get; }

        public MainViewModel(ISteamParserService parserService, IBlockedGamesProvider blockedGamesProvider)
        {
            _parserService = parserService;
            _blockedGamesProvider = blockedGamesProvider;
            RefreshCommand = new Command(async () => await LoadGamesAsync());
            ClearSearchCommand = new Command(() => SearchQuery = string.Empty);
            _ = LoadGamesAsync();
        }

        public async Task LoadGamesAsync()
        {
            _loadCts?.Cancel();
            _loadCts = new CancellationTokenSource();
            var token = _loadCts.Token;

            IsBusy = true;
            IsSearchMode = false;

            try
            {
                string code = GetRegionCode(SelectedRegion);

                bool available = await _parserService.IsRegionAvailableAsync(code);

                if (token.IsCancellationRequested) return;

                IsRegionAvailable = available;

                if (!available)
                {
                    _allGames.Clear();
                    Games.Clear();
                    BlockedGames.Clear();
                    HasBlockedGames = false;
                    return;
                }

                _allGames = await _parserService.GetDiscountsAsync(code, token) ?? new List<SteamGame>();

                if (token.IsCancellationRequested) return;

                ApplySort();

                if (code == "RU")
                    await LoadBlockedGamesAsync();
                else
                {
                    BlockedGames.Clear();
                    HasBlockedGames = false;
                }
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                if (!token.IsCancellationRequested)
                    IsBusy = false;
            }
        }

        private async Task LoadBlockedGamesAsync()
        {
            try
            {
                var blocked = await _blockedGamesProvider.GetBlockedInRussiaAsync();

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    BlockedGames.Clear();
                    foreach (var g in blocked)
                    {
                        g.IsBlocked = true;
                        BlockedGames.Add(g);
                    }
                    HasBlockedGames = BlockedGames.Count > 0;
                    OnPropertyChanged(nameof(BlockedSectionHeaderText));
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }

        private async Task OnSearchQueryChangedAsync(string query)
        {
            _searchCts?.Cancel();

            if (string.IsNullOrWhiteSpace(query))
            {
                IsSearchMode = false;
                ApplySort();
                return;
            }

            _searchCts = new CancellationTokenSource();
            var token = _searchCts.Token;

            try
            {
                await Task.Delay(450, token);
                if (token.IsCancellationRequested) return;

                IsBusy = true;
                IsSearchMode = true;

                string code = GetRegionCode(SelectedRegion);
                var results = await _parserService.SearchGamesAsync(query, code, token);

                if (token.IsCancellationRequested) return;

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Games.Clear();
                    foreach (var g in results)
                        Games.Add(g);
                });
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                if (!token.IsCancellationRequested)
                    IsBusy = false;
            }
        }

        private void ApplySort()
        {
            var sorted = _allGames.AsEnumerable();

            if (SelectedSort == "Цена: по возрастанию")
                sorted = sorted.OrderBy(g => g.PriceValue == 0 ? double.MaxValue : g.PriceValue);
            else if (SelectedSort == "Цена: по убыванию")
                sorted = sorted.OrderByDescending(g => g.PriceValue);
            else
                sorted = sorted.OrderByDescending(g => g.DiscountValue);

            var snapshot = sorted.ToList();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                Games.Clear();
                foreach (var g in snapshot)
                    Games.Add(g);
            });
        }

        private static string GetRegionCode(string region)
        {
            return region switch
            {
                "США" => "US",
                "Украина" => "UA",
                _ => "RU"
            };
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}