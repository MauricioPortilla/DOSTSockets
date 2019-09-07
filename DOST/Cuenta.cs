using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOST {
    class Cuenta {
        private int id;
        private string usuario;
        private string password; // cifrado con SHA1
        private string correo;
        private int monedas;
        private DateTime fechaCreacion;
        private bool confirmada;
        private string codigoValidacion;

        public Cuenta(int id) {
            this.id = id;
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
            Session.Cuenta = new Cuenta(
                int.Parse(packageReceived["idcuenta"]), packageReceived["usuario"], packageReceived["password"],
                packageReceived["correo"], int.Parse(packageReceived["monedas"]),
                DateTime.Parse(packageReceived["fechaCreacion"]), (packageReceived["confirmado"] == "1") ? true : false,
                packageReceived["codigoValidacion"]
            );
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

        public int Id {
            get {
                return id;
            }
            set {
                id = value;
            }
        }
    }
}
