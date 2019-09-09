using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOST {
    class Session {
        private static Cuenta cuenta;
        public static Cuenta Cuenta { get; set; }
        private static MainMenuWindow mainMenu;
        public static MainMenuWindow MainMenu {
            get { return mainMenu; }
            set { mainMenu = value; }
        }
        private static LoginWindow login;
        public static LoginWindow Login {
            get { return login; }
            set { login = value; }
        }
        private static readonly ObservableCollection<Partida> gamesList = new ObservableCollection<Partida>();
        public static ObservableCollection<Partida> GamesList {
            get {
                return gamesList;
            }
        }

        public static void GetGamesList() {
            while (true) {
                EngineNetwork.Send(EngineNetwork.CreatePackage(new object[] {
                    (byte) NetworkClientRequests.GetGames
                }));
                var gamesPackage = EngineNetwork.ReceiveMultipleData();
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
            }
        }

        static void Game_PropertyChanged(object sender, PropertyChangedEventArgs e) {
            mainMenu.gamesListView.Items.Refresh();
        }
    }
}
