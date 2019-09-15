using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DOST {
    class Session {
        public static readonly Dictionary<string, string> LANGUAGES = new Dictionary<string, string>() {
            { "Español", "es-MX" }, { "English", "en-US" }
        };
        private static Cuenta cuenta;
        public static Cuenta Cuenta {
            get { return cuenta; }
            set { cuenta = value; }
        }
        private static LoginWindow login;
        public static LoginWindow Login {
            get { return login; }
            set { login = value; }
        }
        private static MainMenuWindow mainMenu;
        public static MainMenuWindow MainMenu {
            get { return mainMenu; }
            set { mainMenu = value; }
        }
        private static GameLobbyWindow gameLobbyWindow;
        public static GameLobbyWindow GameLobbyWindow {
            get { return gameLobbyWindow; }
            set { gameLobbyWindow = value; }
        }
        private static readonly ObservableCollection<Partida> gamesList = new ObservableCollection<Partida>();
        public static ObservableCollection<Partida> GamesList {
            get {
                return gamesList;
            }
        }
        public static Thread gameThreads = new Thread(GameThreads);

        public static void GameThreads() {
            while (true) {
                if (mainMenu != null) {
                    GetGamesList();
                    mainMenu.JoinGameIfNeeded();
                }
                if (gameLobbyWindow != null) {
                    gameLobbyWindow.LoadPlayersJoinedData();
                    gameLobbyWindow.ReceiveChatMessages();
                }
            }
        }

        public static void GetGamesList() {
            if (cuenta != null) {
                EngineNetwork.Send(EngineNetwork.CreatePackage(new object[] {
                    (byte) NetworkClientRequests.GetGames
                }));
                var gamesPackage = EngineNetwork.ReceiveMultipleData();
                if (gamesPackage.Count == 0) {
                    return;
                }
                if (!gamesPackage[0].ContainsKey("code")) {
                    return;
                } else if (byte.Parse(gamesPackage[0]["code"]) != (byte) NetworkServerResponses.GamesList) {
                    return;
                }
                gamesPackage.RemoveAll(x => x.ContainsKey("code"));
                foreach (var gamePackage in gamesPackage) {
                    System.Windows.Application.Current.Dispatcher.Invoke(delegate {
                        if (!GamesList.ToList().Exists(x => x.Id == int.Parse(gamePackage["idpartida"]))) {
                            var game = new Partida(
                                int.Parse(gamePackage["idpartida"]),
                                int.Parse(gamePackage["ronda"]),
                                Convert.ToDateTime(gamePackage["fecha"])
                            );
                            game.PropertyChanged += Game_PropertyChanged;
                            GamesList.Add(game);
                        } else {
                            GamesList.ToList().Find(x => x.Id == int.Parse(gamePackage["idpartida"])).LoadJugadores();
                        }
                    });
                }
                List<Partida> gamesToRemove = new List<Partida>();
                foreach (var game in GamesList) {
                    var checkGame = gamesPackage.ToList().Find(x => int.Parse(x["idpartida"]) == game.Id);
                    if (checkGame == null) {
                        gamesToRemove.Add(game);
                    }
                }
                System.Windows.Application.Current.Dispatcher.Invoke(delegate {
                    gamesToRemove.ForEach(x => GamesList.Remove(x));
                });
            }
        }

        static void Game_PropertyChanged(object sender, PropertyChangedEventArgs e) {
            mainMenu.gamesListView.Items.Refresh();
        }
    }
}
