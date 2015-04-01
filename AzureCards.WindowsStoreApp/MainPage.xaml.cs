﻿using Microsoft.Azure.AppService;
using System;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace AzureCards.WindowsStoreApp
{
    public sealed partial class MainPage : Page
    {
        private const string GATEWAY = "YOUR GATEWAY HERE";
        private const string API_APP = "YOUR API APP URL HERE";

        private IAppServiceClient _appServiceClient;
        private IAzureCardsClient _azureCards;
        private string _deckId;
        private const string AUTH_PROVIDER = "twitter";

        public MainPageViewModel ViewModel
        {
            get { return this.DataContext as MainPageViewModel; }
            set { this.DataContext = value; }
        }

        public MainPage()
        {
            this.InitializeComponent();
            this.ViewModel = new MainPageViewModel();

            _appServiceClient = new AppServiceClient(
                GATEWAY,
                new TokenExpiredHandler(this.AuthenticateAsync)
                );
        }

        protected async override void OnNavigatedTo(Windows.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            await AuthenticateAsync();

            _azureCards = _appServiceClient.Create(
                new Uri(API_APP),
                new TokenExpiredHandler(AuthenticateAsync)
                );

            RefreshCardDisplay();

            base.OnNavigatedTo(e);
        }

        private async Task<IAppServiceUser> AuthenticateAsync()
        {
            await _appServiceClient.Logout();

            while (_appServiceClient.CurrentUser == null)
            {
                await _appServiceClient.LoginAsync(AUTH_PROVIDER, false);
            }

            return _appServiceClient.CurrentUser;
        }

        private void RefreshCardDisplay()
        {
            var refreshCards = new Action(() =>
            {
                this.ViewModel.Cards.Clear();

                var response = _azureCards.Deck.Deal(_deckId, 5);

                foreach (var card in response.Cards)
                    this.ViewModel.Cards.Add(card.CreateViewModel());
            });

            var startNewDeck = new Action(() =>
            {
                _deckId = _azureCards.Deck.New();

                for (int i = 0; i < 10; i++)
                    _azureCards.Deck.Shuffle(_deckId);

                refreshCards();
            });

            if (string.IsNullOrEmpty(_deckId))
                startNewDeck();

            try
            {
                refreshCards();
            }
            catch
            {
                startNewDeck();
            }
        }

        private void DealTapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            RefreshCardDisplay();
        }
    }
}
