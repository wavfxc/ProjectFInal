using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Project.Models;
using Project.Services;
using Project.ViewModels;

namespace Project
{
    public partial class GameDetailsPage : ContentPage
    {
        public GameDetailsPage(SteamGame game, string countryCode, ISteamParserService parserService)
        {
            InitializeComponent();
            BindingContext = new GameDetailsViewModel(game, countryCode, parserService);
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            RootStack.TranslationY = 24;
            await Task.WhenAll(
                RootStack.FadeTo(1, 280, Easing.CubicOut),
                RootStack.TranslateTo(0, 0, 280, Easing.CubicOut)
            );
        }
    }
}