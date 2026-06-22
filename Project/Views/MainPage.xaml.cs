using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Project.Models;
using Project.Services;
using Project.ViewModels;

namespace Project
{
    public partial class MainPage : ContentPage
    {
        private readonly MainViewModel _viewModel;
        private readonly ISteamParserService _parserService;
        private SteamGame _lastSelectedGame;
        private bool _isNavigating;

        public MainPage(MainViewModel viewModel, ISteamParserService parserService)
        {
            InitializeComponent();
            _viewModel = viewModel;
            _parserService = parserService;
            BindingContext = _viewModel;
        }

        private async void OnGameTapped(object sender, TappedEventArgs e)
        {
            if (_isNavigating) return;
            if (e.Parameter is not SteamGame game) return;

            _isNavigating = true;

            if (_lastSelectedGame != null && _lastSelectedGame != game)
                _lastSelectedGame.IsSelected = false;

            game.IsSelected = true;
            _lastSelectedGame = game;

            if (sender is VisualElement visual)
                await AnimateTapAsync(visual);

            string region = GetCurrentRegionCode();
            var detailsPage = new GameDetailsPage(game, region, _parserService);

            await Navigation.PushAsync(detailsPage);

            game.IsSelected = false;
            _isNavigating = false;
        }

        private static async Task AnimateTapAsync(VisualElement visual)
        {
            await visual.ScaleTo(0.97, 80, Easing.CubicOut);
            await visual.ScaleTo(1.0, 90, Easing.CubicIn);
        }

        private async void OnToggleBlockedSection(object sender, EventArgs e)
        {
            _viewModel.IsBlockedSectionExpanded = !_viewModel.IsBlockedSectionExpanded;

            if (_viewModel.IsBlockedSectionExpanded)
            {
                BlockedGamesPanel.IsVisible = true;
                BlockedGamesPanel.Opacity = 0;
                BlockedGamesPanel.TranslationY = -10;
                await Task.WhenAll(
                    BlockedGamesPanel.FadeTo(1, 220, Easing.CubicOut),
                    BlockedGamesPanel.TranslateTo(0, 0, 220, Easing.CubicOut)
                );
            }
            else
            {
                await Task.WhenAll(
                    BlockedGamesPanel.FadeTo(0, 180, Easing.CubicIn),
                    BlockedGamesPanel.TranslateTo(0, -10, 180, Easing.CubicIn)
                );
                BlockedGamesPanel.IsVisible = false;
            }
        }

        private string GetCurrentRegionCode()
        {
            return _viewModel.SelectedRegion switch
            {
                "США" => "US",
                "Украина" => "UA",
                _ => "RU"
            };
        }
    }
}