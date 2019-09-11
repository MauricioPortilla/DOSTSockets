using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOST {
    public class Partida : INotifyPropertyChanged {
        private int id;
        public int Id {
            get {
                return id;
            }
        }
        private int ronda;
        private DateTime fecha;
        private List<Jugador> jugadores;
        public List<Jugador> Jugadores {
            get {
                return jugadores;
            }
        }
        public string Nombre { get; }
        private int numeroJugadores;
        public string NumeroJugadores {
            get {
                return numeroJugadores + "/4";
            } set {
                numeroJugadores = int.Parse(value);
                NotifyPropertyChanged("NumeroJugadores");
            }
        }

        public Partida(int id, int ronda, DateTime fecha) {
            this.id = id;
            this.ronda = ronda;
            this.fecha = fecha;
            jugadores = new List<Jugador>();
            LoadJugadores();
            Nombre = "Partida de " + jugadores.Find(x => x.Anfitrion == true).Cuenta.Usuario;
        }

        public void LoadJugadores() {
            jugadores.Clear();
            EngineNetwork.Send(EngineNetwork.CreatePackage(new object[] {
                (byte) NetworkClientRequests.GetGamePlayers, id
            }));
            List<Dictionary<string, string>> players = EngineNetwork.ReceiveMultipleData();
            foreach (var player in players) {
                jugadores.Add(new Jugador(
                    int.Parse(player["idjugador"]), 
                    new Cuenta(int.Parse(player["idcuenta"])),
                    this,
                    int.Parse(player["puntuacion"]),
                    (player["anfitrion"] == "1") ? true : false
                ));
            }
            if (numeroJugadores != jugadores.Count) {
                NumeroJugadores = jugadores.Count.ToString();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string obj) {
            if (PropertyChanged != null) {
                this.PropertyChanged(this, new PropertyChangedEventArgs(obj));
            }
        }
    }
}
