using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace DOST {
    /// <summary>
    /// Lógica de interacción para MainMenuWindow.xaml
    /// </summary>
    public partial class MainMenuWindow : Window {
        private ObservableCollection<Partida> _gamesList = Session.GamesList;
        private Thread gamesListThread;
        public ObservableCollection<Partida> GamesList {
            get {
                return _gamesList;
            }
        }

        public MainMenuWindow() {
            DataContext = this;
            InitializeComponent();
            usernameTextBlock.Text = Session.Cuenta.Usuario;
            coinsTextBlock.Text = " " + Session.Cuenta.Monedas.ToString();
            // rankTextBlock.Text = Session.Cuenta.GetRank();
            gamesListThread = new Thread(Session.GetGamesList);
            gamesListThread.Start();
            GamesList.CollectionChanged += GamesList_CollectionChanged;
        }

        void GamesList_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            gamesListView.Items.Refresh();
        }

        private void JoinGameButton_Click(object sender, RoutedEventArgs e) {
            if (gamesListView.SelectedItem == null) {
                return;
            }
            var selectedGame = (Partida) gamesListView.SelectedItem;
            if (Session.Cuenta.JoinGame(selectedGame)) {
                GameLobbyWindow gameLobby = new GameLobbyWindow(ref selectedGame);
                gameLobby.Show();
                gamesListThread.Abort();
                Close();
            }
        }

        private void BestScoresButton_Click(object sender, RoutedEventArgs e) {

        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e) {
            Session.Cuenta.Logout();
            Session.Cuenta = null;
            Session.MainMenu = null;
            Session.Login.Show();
            Close();
        }
    }
}
