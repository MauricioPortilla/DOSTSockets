using System;
using System.Collections.Generic;
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
    /// Lógica de interacción para GameLobbyWindow.xaml
    /// </summary>
    public partial class GameLobbyWindow : Window {
        private Partida partida;
        private List<TextBlock> lobbyPlayersUsernameTextBlocks;
        private List<TextBlock> lobbyPlayersTypeTextBlocks;
        private List<TextBlock> lobbyPlayersRankTextBlocks;
        private List<TextBlock> lobbyPlayersRankTitleTextBlocks;
        private static readonly int MAX_NUMBER_OF_PLAYERS = 4;

        public GameLobbyWindow(ref Partida partida) {
            InitializeComponent();
            this.partida = partida;
            lobbyPlayersUsernameTextBlocks = new List<TextBlock>() {
                playerOneUsernameTextBlock, playerTwoUsernameTextBlock,
                playerThreeUsernameTextBlock, playerFourUsernameTextBlock
            };
            lobbyPlayersTypeTextBlocks = new List<TextBlock>() {
                playerOneTypeTextBlock, playerTwoTypeTextBlock,
                playerThreeTypeTextBlock, playerFourTypeTextBlock
            };
            lobbyPlayersRankTextBlocks = new List<TextBlock>() {
                playerOneRankTextBlock, playerTwoRankTextBlock,
                playerThreeRankTextBlock, playerFourRankTextBlock
            };
            lobbyPlayersRankTitleTextBlocks = new List<TextBlock>() {
                playerOneRankTitleTextBlock, playerTwoRankTitleTextBlock,
                playerThreeRankTitleTextBlock, playerFourRankTitleTextBlock
            };

            Thread loadPlayersJoinedDataThread = new Thread(LoadPlayersJoinedData);
            loadPlayersJoinedDataThread.Start();
            /*Thread receiveChatMessagesThread = new Thread(ReceiveChatMessages);
            receiveChatMessagesThread.Start();*/
        }

        private void LoadPlayersJoinedData() {
            Application.Current.Dispatcher.Invoke(delegate {
                //while (true) {
                    if (!partida.Jugadores.Find(x => x.Cuenta.Id == Session.Cuenta.Id).Anfitrion) {
                        startGameButton.Content = Properties.Resources.ReadyButton;
                    }
                    for (int index = 0; index < partida.Jugadores.Count; index++) {
                        lobbyPlayersUsernameTextBlocks[index].Text = partida.Jugadores[index].Cuenta.Usuario;
                        lobbyPlayersTypeTextBlocks[index].Text = partida.Jugadores[index].Anfitrion ?
                            Properties.Resources.HostPlayerText : Properties.Resources.PlayerText;
                        // lobbyPlayersRankTextBlocks[index].Text = partida.Jugadores[index].GetRank();
                        lobbyPlayersRankTextBlocks[index].Visibility = Visibility.Visible;
                        lobbyPlayersRankTitleTextBlocks[index].Visibility = Visibility.Visible;
                    }
                    if (partida.Jugadores.Count == MAX_NUMBER_OF_PLAYERS) {
                        lobbyStatusTextBlock.Text = "";
                    } else {
                        lobbyStatusTextBlock.Text = Properties.Resources.WaitingForPlayersText;
                    }
                //}
            });
        }

        private void ReceiveChatMessages() {
            while (true) {
                var messagePackage = EngineNetwork.ReceiveAsDictionary();
                if (!messagePackage.ContainsKey("code")) {
                    continue;
                }
                if (byte.Parse(messagePackage["code"]) == (byte) NetworkServerResponses.ChatMessage) {
                    Application.Current.Dispatcher.Invoke(delegate {
                        chatListBox.Items.Add(new TextBlock() {
                            Text = messagePackage["username"] + ": " + messagePackage["message"]
                        });
                    });
                }
            }
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e) {

        }

        private void StartGameButton_Click(object sender, RoutedEventArgs e) {

        }

        private void ConfigurationButton_Click(object sender, RoutedEventArgs e) {

        }

        private void ChatMessageTextBox_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) {
                Session.Cuenta.SendChatMessage(partida, chatMessageTextBox.Text);
                chatMessageTextBox.Clear();
            }
        }
    }
}
