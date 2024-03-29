﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOST {
    public class Cuenta {
        private int id;
        public int Id { get { return id; } set { id = value; } }
        private string usuario;
        public string Usuario { get { return usuario; } set { usuario = value; } }
        private string password; // cifrada con SHA1
        public string Password { get { return password; } set { password = value; } }
        private string correo;
        public string Correo { get { return correo; } set { correo = value; } }
        private int monedas;
        public int Monedas { get { return monedas; } set { monedas = value; } }
        private DateTime fechaCreacion;
        private bool confirmada;
        private string codigoValidacion;

        public Cuenta(int id) {
            this.id = id;
            EngineNetwork.Send(EngineNetwork.CreatePackage(new object[] {
                (byte) NetworkClientRequests.GetAccountData, id
            }));
            var package = EngineNetwork.ReceiveMultipleData();
            if (package.Count < 1) {
                return;
            }
            if (!package[0].ContainsKey("code")) {
                return;
            } else if (byte.Parse(package[0]["code"]) != (byte) NetworkServerResponses.AccountData) {
                return;
            }
            package.RemoveAll(x => x.ContainsKey("code"));
            var accountData = package[0];
            if (accountData.Count > 0) {
                id = int.Parse(accountData["idcuenta"]);
                usuario = accountData["usuario"];
                password = accountData["password"];
                correo = accountData["correo"];
                monedas = int.Parse(accountData["monedas"]);
                fechaCreacion = DateTime.Parse(accountData["fechaCreacion"]);
                confirmada = (accountData["confirmado"] == "1") ? true : false;
                codigoValidacion = accountData["codigoValidacion"];
            }
        }

        public Cuenta(string usuario, string password) {
            this.usuario = usuario;
            this.password = password;
        }

        public Cuenta(
            int id, string usuario, string password, string correo, int monedas, 
            DateTime fechaCreacion, bool confirmada, string codigoValidacion
        ) {
            this.id = id;
            this.usuario = usuario;
            this.password = password;
            this.correo = correo;
            this.monedas = monedas;
            this.fechaCreacion = fechaCreacion;
            this.confirmada = confirmada;
            this.codigoValidacion = codigoValidacion;
        }

        public bool Login(out byte codeResponse) {
            var loginRequest = EngineNetwork.CreatePackage(new object[] {
                (byte) NetworkClientRequests.Login, usuario, password
            });
            EngineNetwork.Send(loginRequest);
            Dictionary<string, string> packageReceived = EngineNetwork.ReceiveAsDictionary();
            codeResponse = (byte) NetworkServerResponses.LoginError;
            if (packageReceived.Count == 0) {
                return false;
            } else if (packageReceived.Count == 1) {
                if (byte.Parse(packageReceived["code"]) == (byte) NetworkServerResponses.LoginError) {
                    return false;
                } else if (byte.Parse(packageReceived["code"]) == (byte) NetworkServerResponses.AccountNotConfirmed) {
                    codeResponse = (byte) NetworkServerResponses.AccountNotConfirmed;
                    return false;
                }
            }
            Session.Cuenta.id = int.Parse(packageReceived["idcuenta"]);
            Session.Cuenta.usuario = packageReceived["usuario"];
            Session.Cuenta.password = packageReceived["password"];
            Session.Cuenta.correo = packageReceived["correo"];
            Session.Cuenta.monedas = int.Parse(packageReceived["monedas"]);
            Session.Cuenta.fechaCreacion = DateTime.Parse(packageReceived["fechaCreacion"]);
            Session.Cuenta.confirmada = (packageReceived["confirmado"] == "1") ? true : false;
            Session.Cuenta.codigoValidacion = packageReceived["codigoValidacion"];
            codeResponse = 1;
            return true;
        }

        public bool Register() {
            if (!string.IsNullOrWhiteSpace(codigoValidacion)) {
                return false;
            }
            var registerRequest = EngineNetwork.CreatePackage(new object[] {
                (byte) NetworkClientRequests.Register, usuario, password, correo
            });
            EngineNetwork.Send(registerRequest);
            Dictionary<string, string> packageReceived = EngineNetwork.ReceiveAsDictionary();
            if (packageReceived.Count == 0) {
                return false;
            } else if (packageReceived.Count == 1) {
                if (byte.Parse(packageReceived["code"]) != (byte) NetworkServerResponses.RegisterSuccess) {
                    return false;
                }
            }
            return true;
        }

        public void Logout() {
            var logoutRequest = EngineNetwork.CreatePackage(new object[] {
                (byte) NetworkClientRequests.Logout, id
            });
            EngineNetwork.Send(logoutRequest);
        }

        public bool JoinGame(Partida game) {
            var joinGameRequest = EngineNetwork.CreatePackage(new object[] {
                (byte) NetworkClientRequests.JoinGame, id, game.Id, 
            });
            EngineNetwork.Send(joinGameRequest);
            List<string> packageReceived = EngineNetwork.Receive();
            if (packageReceived.Count == 0) {
                return false;
            } else if (packageReceived.Count == 1) {
                if (byte.Parse(packageReceived[0]) != (byte) NetworkServerResponses.PlayerJoined) {
                    return false;
                }
            }
            return true;
        }

        public void SendChatMessage(Partida game, string message) {
            var sendChatMessageRequest = EngineNetwork.CreatePackage(new object[] {
                (byte) NetworkClientRequests.SendChatMessage, game.Id, usuario, message
            });
            EngineNetwork.Send(sendChatMessageRequest);
        }

        public bool LeaveGame(Partida game) {
            EngineNetwork.Send(EngineNetwork.CreatePackage(new object[] {
                (byte) NetworkClientRequests.LeaveGame, id, game.Id
            }));
            var packageReceived = EngineNetwork.ReceiveMultipleData();
            if (packageReceived.Count == 0) {
                return false;
            } else if (packageReceived.Count == 1) {
                if (byte.Parse(packageReceived[0]["code"]) != (byte) NetworkServerResponses.PlayerLeft) {
                    return false;
                }
            }
            return true;
        }

        public bool CreateGame() {
            if (Session.GamesList.ToList().Find(x => x.Jugadores.Find(p => p.Cuenta.id == id) != null) != null) {
                return false;
            }
            EngineNetwork.Send(EngineNetwork.CreatePackage(new object[] {
                (byte) NetworkClientRequests.CreateGame, id
            }));
            var packageReceived = EngineNetwork.ReceiveMultipleData();
            if (packageReceived.Count == 0) {
                return false;
            } else if (packageReceived.Count == 1) {
                if (byte.Parse(packageReceived[0]["code"]) != (byte) NetworkServerResponses.GameCreated) {
                    return false;
                }
            }
            return true;
        }
    }
}
