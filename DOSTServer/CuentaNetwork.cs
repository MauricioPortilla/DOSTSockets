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
                using (StreamReader dostStreamReader = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding(response.CharacterSet))) {
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
                    "Da clic <a href=\"https://www.arkanapp.com/dost.php?validationcode=" + codigoValidacion + "\" target=\"_blank\">AQUÍ</a> para activar tu cuenta." +
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
    }
}
