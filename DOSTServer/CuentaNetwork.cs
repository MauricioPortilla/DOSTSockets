using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Xml.Linq;
using static DOSTServer.DatabaseStruct;

namespace DOSTServer {
    static class CuentaNetwork {
        /// <summary>
        /// Verifica si es posible iniciar sesión con los datos proporcionados.
        /// </summary>
        /// <param name="username">Nombre de usuario</param>
        /// <param name="password">Contraseña</param>
        /// <returns>Una lista con los datos de la cuenta encontrada.</returns>
        public static Dictionary<string, object> TryLogin(string username, string password) {
            string hashedPassword = Engine.HashWithSHA256(password);
            Dictionary<string, object> accountData = new Dictionary<string, object>();
            Database.ExecuteStoreQuery(
                "SELECT * FROM cuenta WHERE usuario = @username AND password = @password",
                new Dictionary<string, object>() {
                    { "@username", username }, { "@password", hashedPassword }
                }, (results) => {
                    SSQLRow row = results[0];
                    if ((short) row.Columns["confirmado"] == 0) {
                        if (!TryValidateAccount(row.Columns["codigoValidacion"].ToString())) {
                            accountData.Add("confirmado", (short) 0);
                            return;
                        } else {
                            Database.ExecuteUpdate(
                                "UPDATE cuenta SET confirmado = 1 WHERE idcuenta = @idcuenta", 
                                new Dictionary<string, object>() {
                                    { "@idcuenta", (int) row.Columns["idcuenta"] }
                                }
                            );
                            row.Columns["confirmado"] = (short) 1;
                        }
                    }
                    foreach (KeyValuePair<string, object> column in row.Columns) {
                        accountData.Add(column.Key, column.Value);
                    }
                }
            );
            return accountData;
        }

        public static bool TryRegister(string username, string password, string email) {
            string hashedPassword = Engine.HashWithSHA256(password);
            var accountData = Database.ExecuteStoreQuery(
                "SELECT idcuenta FROM cuenta WHERE usuario = @username OR correo = @email",
                new Dictionary<string, object>() {
                    { "@username", username }, { "@email", email }
                }, (results) => {
                    return;
                }, null
            );
            if (accountData.Count > 0) {
                return false;
            }
            string codigoValidacion = Engine.HashWithSHA256(Guid.NewGuid().ToString() + DateTime.Now.Ticks);
            bool registerAccount = Database.ExecuteUpdate(
                "INSERT INTO cuenta VALUES (@username, @password, @correo, 0, 0, @fechaCreacion, @codigoValidacion)",
                new Dictionary<string, object>() {
                    { "@username", username }, { "@password", hashedPassword }, { "@correo", email },
                    { "@fechaCreacion", DateTime.Now }, { "@codigoValidacion", codigoValidacion }
                }, null
            );
            if (registerAccount) {
                SendSignUpEmail(email, codigoValidacion);
            }
            return registerAccount;
        }

        private static bool TryValidateAccount(string codigoValidacion) {
            HttpWebResponse response = null;
            string result = string.Empty;
            try {
                HttpWebRequest dostWebRequest = (HttpWebRequest) WebRequest.Create(
                    "https://www.arkanapp.com/dost/dost.php?checkValidationCode=" + codigoValidacion
                );
                dostWebRequest.Method = "GET";
                response = (HttpWebResponse) dostWebRequest.GetResponse();
                using (StreamReader dostStreamReader = new StreamReader(
                    response.GetResponseStream(), Encoding.GetEncoding(response.CharacterSet)
                )) {
                    result = dostStreamReader.ReadToEnd();
                }
            } catch (WebException webException) {
                Console.WriteLine("Error web request: " + webException.Message);
            } finally {
                if (response != null) {
                    response.Close();
                }
            }
            return string.IsNullOrWhiteSpace(result) ? false : ((result == "0") ? false : true);
        }

        private static bool SendSignUpEmail(string email, string codigoValidacion) {
            var xmlElements = Server.GetConfigFileElements();
            try {
                SmtpClient client = new SmtpClient(xmlElements["Smtp"]["SMTPServer"]);
                var mail = new MailMessage();
                mail.From = new MailAddress(xmlElements["Smtp"]["Email"], "DOST");
                mail.To.Add(email);
                mail.Subject = "Activa tu cuenta en DOST";
                mail.IsBodyHtml = true;
                mail.Body = "<h3>¡Bienvenido a DOST!</h3><br>" +
                    "Da clic <a href=\"https://www.arkanapp.com/dost/dost.php?validationcode=" + 
                    codigoValidacion + "\" target=\"_blank\">AQUÍ</a> para activar tu cuenta." +
                    "<br><br>¡Diviértete!<br>-El equipo de DOST";
                int port = 587;
                if (int.TryParse(xmlElements["Smtp"]["Port"], out port)) {
                    client.Port = port;
                } else {
                    client.Port = 587;
                }
                client.UseDefaultCredentials = false;
                client.Credentials = new NetworkCredential(xmlElements["Smtp"]["Email"], xmlElements["Smtp"]["EmailPassword"]);
                client.EnableSsl = true;
                client.Send(mail);
                return true;
            } catch (Exception) {
                return false;
            }
        }

        public static List<Dictionary<string, object>> GetAccountData(int idcuenta) {
            //var accountData = new Dictionary<string, object>();
            var accountData = new List<Dictionary<string, object>>() {
                { new Dictionary<string, object>() {
                    { "code", (byte) NetworkServerResponses.AccountData }
                } }
            };
            Database.ExecuteStoreQuery(
                "SELECT * FROM cuenta WHERE idcuenta = @idcuenta",
                new Dictionary<string, object>() {
                    { "@idcuenta", idcuenta }
                }, (results) => {
                    var row = results[0];
                    Dictionary<string, object> data = new Dictionary<string, object>();
                    foreach (var columnData in row.Columns) {
                        data.Add(columnData.Key, columnData.Value);
                    }
                    accountData.Add(data);
                }
            );
            return accountData;
        }

        public static void Logout(int idcuenta) {
            Database.ExecuteUpdate(
                "DELETE FROM jugador WHERE idcuenta = @idcuenta AND " +
                "idpartida IN (SELECT idpartida FROM partida WHERE ronda < 5)",
                new Dictionary<string, object>() {
                    { "@idcuenta", idcuenta }
                }
            );
        }

        public static bool JoinGame(int idcuenta, int idpartida, bool anfitrion) {
            return Database.ExecuteUpdate(
                "INSERT INTO jugador VALUES (@idcuenta, @idpartida, 0, @anfitrion)",
                new Dictionary<string, object>() {
                    { "@idcuenta", idcuenta }, { "@idpartida", idpartida },
                    { "@anfitrion", (anfitrion == true) ? 1 : 0 }
                }
            );
        }

        public static bool LeaveGame(int idcuenta, int idpartida) {
            var players = PartidaNetwork.GetPlayersData(idpartida);
            players.RemoveAll(x => x.ContainsKey("code"));
            var player = players.Find(x => (int) x["idcuenta"] == idcuenta);
            if (player == null) {
                return false;
            }
            return Database.ExecuteUpdate(
                "IF EXISTS (SELECT idjugador FROM jugador WHERE idjugador = @idjugador AND idpartida = @idpartida AND anfitrion = 1) " +
                "BEGIN " +
                    "DELETE FROM jugador WHERE idjugador = @idjugador AND idpartida = @idpartida; " +
                    "IF NOT EXISTS (SELECT TOP(1) idjugador FROM jugador WHERE idpartida = @idpartida) " +
                    "BEGIN " +
                        "DELETE FROM partida WHERE idpartida = @idpartida; " +
                    "END " +
                    "ELSE " +
                    "BEGIN " +
                        "UPDATE TOP(1) jugador SET anfitrion = 1 WHERE idpartida = @idpartida; " +
                    "END " +
                "END " +
                "ELSE IF EXISTS (SELECT idjugador FROM jugador WHERE idjugador = @idjugador AND idpartida = @idpartida) " +
                "BEGIN " +
                    "DELETE FROM jugador WHERE idjugador = @idjugador AND idpartida = @idpartida; " +
                    "IF NOT EXISTS (SELECT TOP(1) idjugador FROM jugador WHERE idpartida = @idpartida) " +
                    "BEGIN " +
                        "DELETE FROM partida WHERE idpartida = @idpartida; " +
                    "END " +
                "END",
                new Dictionary<string, object>() {
                    { "@idjugador", (int) player["idjugador"] },
                    { "@idpartida", idpartida }
                }
            );
        }

        public static bool CreateGame(int idcuenta) {
            int idpartida = 0;
            if (Database.ExecuteUpdate(
                "INSERT INTO partida OUTPUT INSERTED.idpartida VALUES (0, @fecha)",
                new Dictionary<string, object>() {
                    { "@fecha", DateTime.Now }
                }, out idpartida
            )) {
                return JoinGame(idcuenta, idpartida, true);
            }
            return false;
        }
    }
}
