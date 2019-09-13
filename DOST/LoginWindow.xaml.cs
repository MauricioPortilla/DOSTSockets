using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DOST {
    /// <summary>
    /// Lógica de interacción para LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window {
        public LoginWindow() {
            InitializeComponent();
            var language = Session.LANGUAGES.FirstOrDefault(x => x.Value == App.GetAppConfiguration()["DOST"]["Language"]).Key;
            languageSelectorComboBox.SelectedValue = language;
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e) {
            if (string.IsNullOrWhiteSpace(usernameTextBox.Text) || string.IsNullOrWhiteSpace(passwordPasswordBox.Password)) {
                MessageBox.Show("Faltan campos por completar");
                return;
            }
            EngineNetwork.EstablishConnection();
            Session.Cuenta = new Cuenta(usernameTextBox.Text, passwordPasswordBox.Password);
            byte codeResponse = 0;
            if (Session.Cuenta.Login(out codeResponse)) {
                MainMenuWindow mainMenu = new MainMenuWindow();
                Session.MainMenu = mainMenu;
                Session.Login = this;
                passwordPasswordBox.Password = "";
                mainMenu.Show();
                Hide();
            } else {
                switch (codeResponse) {
                    case (byte) NetworkServerResponses.AccountNotConfirmed:
                        MessageBox.Show("Debes confirmar tu cuenta para iniciar sesión. Verifica tu correo electrónico.");
                        break;
                    case (byte) NetworkServerResponses.LoginError:
                        MessageBox.Show("No existe una cuenta registrada con esos datos.");
                        break;
                }
            }
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e) {
            var registerWindow = new RegisterWindow();
            registerWindow.Show();
        }

        private void LanguageSelectorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            App.ChangeLanguage(Session.LANGUAGES[((ListBoxItem) e.AddedItems[0]).Content.ToString()]);
            MessageBox.Show("Idioma cambiado. Reinicie el juego para aplicar los cambios.");
        }
    }
}
