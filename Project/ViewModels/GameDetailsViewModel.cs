using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Project.Models;
using Project.Services;

namespace Project.ViewModels
{
    public class GameDetailsViewModel : INotifyPropertyChanged
    {
        private readonly ISteamParserService _parserService;
        private readonly string _countryCode;

        public SteamGame Game { get; }
        public ObservableCollection<string> Screenshots { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> Genres { get; } = new ObservableCollection<string>();

        private string _description;
        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(); }
        }

        private string _trailerUrl;
        public string TrailerUrl
        {
            get => _trailerUrl;
            set { _trailerUrl = value; OnPropertyChanged(); }
        }

        private bool _hasTrailer;
        public bool HasTrailer
        {
            get => _hasTrailer;
            set { _hasTrailer = value; OnPropertyChanged(); }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        private bool _isBlocked;
        public bool IsBlocked
        {
            get => _isBlocked;
            set { _isBlocked = value; OnPropertyChanged(); }
        }

        private string _currentPrice;
        public string CurrentPrice
        {
            get => _currentPrice;
            set { _currentPrice = value; OnPropertyChanged(); }
        }

        private string _originalPrice;
        public string OriginalPrice
        {
            get => _originalPrice;
            set { _originalPrice = value; OnPropertyChanged(); }
        }

        private string _metacriticScore;
        public string MetacriticScore
        {
            get => _metacriticScore;
            set { _metacriticScore = value; OnPropertyChanged(); }
        }

        private bool _hasScreenshots;
        public bool HasScreenshots
        {
            get => _hasScreenshots;
            set { _hasScreenshots = value; OnPropertyChanged(); }
        }

        public GameDetailsViewModel(SteamGame game, string countryCode, ISteamParserService parserService)
        {
            Game = game;
            _countryCode = countryCode;
            _parserService = parserService;
            CurrentPrice = game.CurrentPrice;
            OriginalPrice = game.OriginalPrice;
            _ = LoadDetailsAsync();
        }

        private async Task LoadDetailsAsync()
        {
            IsLoading = true;

            try
            {
                var details = await _parserService.GetGameDetailsAsync(Game.AppId, _countryCode);

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (details == null)
                    {
                        Description = "Описание отсутствует.";
                        return;
                    }

                    IsBlocked = details.IsBlocked;
                    Description = string.IsNullOrWhiteSpace(details.Description)
                        ? "Описание отсутствует."
                        : details.Description;

                    if (!string.IsNullOrWhiteSpace(details.CurrentPrice))
                        CurrentPrice = details.CurrentPrice;

                    if (!string.IsNullOrWhiteSpace(details.OriginalPrice))
                        OriginalPrice = details.OriginalPrice;

                    MetacriticScore = details.MetacriticScore;

                    Genres.Clear();
                    foreach (var genre in details.Genres)
                        Genres.Add(genre);

                    Screenshots.Clear();
                    foreach (var screenshot in details.Screenshots)
                        Screenshots.Add(screenshot);
                    HasScreenshots = Screenshots.Count > 0;

                    TrailerUrl = details.TrailerUrl;
                    HasTrailer = !string.IsNullOrWhiteSpace(details.TrailerUrl);
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                MainThread.BeginInvokeOnMainThread(() => Description = "Не удалось загрузить описание.");
            }
            finally
            {
                MainThread.BeginInvokeOnMainThread(() => IsLoading = false);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}