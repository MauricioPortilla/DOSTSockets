﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Linq;

namespace DOSTServer {
    enum NetworkClientRequests { // 0 a 50
        Logout = 0x00,
        Login = 0x01,
        Register = 0x02,
        GetGames = 0x03,
        GetAccountData = 0x04,
        GetGamePlayers = 0x05,
        JoinGame = 0x06,
        SendChatMessage = 0x07,
        LeaveGame = 0x08,
        CreateGame = 0x09
    }
    enum NetworkServerResponses { // 50 en adelante
        LoginError = 0x33,
        RegisterError = 0x34,
        RegisterSuccess = 0x35,
        AccountNotConfirmed = 0x36,
        PlayerJoined = 0x37,
        ChatMessage = 0x38,
        GamePlayersList = 0x39,
        GamesList = 0x40,
        PlayerLeft = 0x41,
        PlayerLeaveError = 0x42,
        KeepAlive = 0x43,
        AccountData = 0x44,
        GameCreated = 0x45,
        CreateGameError = 0x46
    }

    class EngineNetwork {
        private static readonly int MAX_NUMBER_CONNECTIONS = 4;
        private static readonly Dictionary<Socket, int> clients = new Dictionary<Socket, int>();

        public static void InitializeServer() {
            Console.WriteLine("DOST Server");
            Console.WriteLine(">> Establishing connection...\n");

            IPHostEntry ipHost = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddr = ipHost.AddressList[0];
            IPEndPoint localEndPoint = new IPEndPoint(ipAddr, 25618);

            Socket listener = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            try {
                listener.Bind(localEndPoint);
                listener.Listen(MAX_NUMBER_CONNECTIONS);
                
                Console.WriteLine(">> Server is online.");
                new Thread(KeepClientsAlive).Start();
                while (true) {
                    Socket clientSocket = listener.Accept();
                    clients.Add(clientSocket, 0);
                    Console.WriteLine(">> Client connected: " + (clientSocket.RemoteEndPoint as IPEndPoint).Address.ToString());

                    Thread clientRequestsThread = new Thread(ProcessClientRequests);
                    clientRequestsThread.Start(clientSocket);
                }
            } catch (Exception e) {
                Console.WriteLine(e.ToString());
            }
        }

        private static void ProcessClientRequests(object client) {
            Socket clientSocket = (Socket) client;
            while (true) {
                byte[] bufferMsg = new byte[1024];
                int numBytes = clientSocket.Receive(bufferMsg);
                List<string> content = OpenPackage(bufferMsg, numBytes);
                byte codeRequest = byte.Parse(content[0]);
                if (codeRequest == (byte) NetworkClientRequests.Logout) {
                    ClientLogoutRequest(clientSocket, content);
                    break;
                }
                switch (codeRequest) {
                    case (byte) NetworkClientRequests.Login:
                        ClientLoginRequest(clientSocket, content);
                        break;
                    case (byte) NetworkClientRequests.Register:
                        ClientRegisterRequest(clientSocket, content);
                        break;
                    case (byte) NetworkClientRequests.GetGames:
                        ClientGetGamesRequest(clientSocket);
                        break;
                    case (byte) NetworkClientRequests.GetAccountData:
                        ClientGetAccountData(clientSocket, content);
                        break;
                    case (byte) NetworkClientRequests.GetGamePlayers:
                        ClientGetGamePlayers(clientSocket, content);
                        break;
                    case (byte) NetworkClientRequests.JoinGame:
                        ClientJoinGameRequest(clientSocket, content);
                        break;
                    case (byte) NetworkClientRequests.SendChatMessage:
                        ClientSendChatMessageRequest(clientSocket, content);
                        break;
                    case (byte) NetworkClientRequests.LeaveGame:
                        ClientLeaveGameRequest(clientSocket, content);
                        break;
                    case (byte) NetworkClientRequests.CreateGame:
                        ClientCreateGameRequest(clientSocket, content);
                        break;
                }
            }
            clientSocket.Shutdown(SocketShutdown.Both);
            clientSocket.Close();
            clients.Remove(clientSocket);
            Console.WriteLine(">> User has logged out.");
        }

        private static void ClientLoginRequest(Socket clientSocket, List<string> content) {
            Dictionary<string, object> accountData = CuentaNetwork.TryLogin(content[1], content[2]);
            if (accountData.Count == 0) {
                IList<ArraySegment<byte>> serverResponse = CreateServerResponse(NetworkServerResponses.LoginError);
                Send(clientSocket, serverResponse);
            } else {
                if ((short) accountData["confirmado"] == 0) {
                    IList<ArraySegment<byte>> serverResponse = CreateServerResponse(NetworkServerResponses.AccountNotConfirmed);
                    Send(clientSocket, serverResponse);
                    return;
                }
                IList<ArraySegment<byte>> accountDataPackage = CreatePackage(accountData);
                Send(clientSocket, accountDataPackage);
                Console.WriteLine(">> " + accountData["usuario"] + " has logged in.");
                clients[clientSocket] = (int) accountData["idcuenta"];
            }
        }

        private static void ClientRegisterRequest(Socket clientSocket, List<string> content) {
            var networkServerResponse = NetworkServerResponses.RegisterSuccess;
            if (!CuentaNetwork.TryRegister(content[1], content[2], content[3])) {
                networkServerResponse = NetworkServerResponses.RegisterError;
            }
            IList<ArraySegment<byte>> serverResponse = CreateServerResponse(networkServerResponse);
            Send(clientSocket, serverResponse);
        }

        private static void ClientGetGamesRequest(Socket clientSocket) {
            var gamesDataPackage = CreatePackage(PartidaNetwork.GetGames());
            Send(clientSocket, gamesDataPackage);
        }

        private static void ClientGetAccountData(Socket clientSocket, List<string> content) {
            var accountDataPackage = CreatePackage(CuentaNetwork.GetAccountData(int.Parse(content[1])));
            Send(clientSocket, accountDataPackage);
        }

        private static void ClientGetGamePlayers(Socket clientSocket, List<string> content) {
            var playersDataPackage = CreatePackage(PartidaNetwork.GetPlayersData(int.Parse(content[1])));
            Send(clientSocket, playersDataPackage);
        }

        private static void ClientLogoutRequest(Socket clientSocket, List<string> content) {
            CuentaNetwork.Logout(int.Parse(content[1]));
        }

        private static void ClientJoinGameRequest(Socket clientSocket, List<string> content) {
            CuentaNetwork.JoinGame(int.Parse(content[1]), int.Parse(content[2]), false);
            Send(clientSocket, CreatePackage(new object[] {
                (byte) NetworkServerResponses.PlayerJoined
            }));
        }

        private static void ClientSendChatMessageRequest(Socket clientSocket, List<string> content) {
            // [codigo, idpartida, usuario, mensaje]
            var playersData = PartidaNetwork.GetPlayersData(int.Parse(content[1]));
            var chatMessagePackage = CreatePackage(new List<Dictionary<string, object>>() {
                { new Dictionary<string, object>() {
                    { "code", (byte) NetworkServerResponses.ChatMessage }, { "username", content[2] }, { "message", content[3] }
                } }
            });
            playersData.RemoveAll(x => x.ContainsKey("code"));
            foreach (var playerData in playersData) {
                if (clients.ContainsValue((int) playerData["idcuenta"])) {
                    Send(clients.FirstOrDefault(x => x.Value == (int) playerData["idcuenta"]).Key, chatMessagePackage);
                }
            }
        }

        private static void ClientLeaveGameRequest(Socket clientSocket, List<string> content) {
            var serverResponse = (byte) NetworkServerResponses.PlayerLeft;
            if (!CuentaNetwork.LeaveGame(int.Parse(content[1]), int.Parse(content[2]))) {
                serverResponse = (byte) NetworkServerResponses.PlayerLeaveError;
            }
            Send(clientSocket, CreatePackage(new List<Dictionary<string, object>>() {
                { new Dictionary<string, object>() {
                    { "code", serverResponse }
                } }
            }));
        }

        private static void ClientCreateGameRequest(Socket clientSocket, List<string> content) {
            var serverResponse = (byte) NetworkServerResponses.GameCreated;
            if (!CuentaNetwork.CreateGame(int.Parse(content[1]))) {
                serverResponse = (byte) NetworkServerResponses.CreateGameError;
            }
            Send(clientSocket, CreatePackage(new List<Dictionary<string, object>>() {
                { new Dictionary<string, object>() {
                    { "code", serverResponse }
                } }
            }));
        }

        private static void KeepClientsAlive() {
            while (true) {
                Thread.Sleep(500);
                try {
                    var clientsTemp = clients;
                    foreach (var client in clientsTemp) {
                        if (client.Value == 0) {
                            continue;
                        }
                        Send(client.Key, CreatePackage(new List<Dictionary<string, object>>() {
                            { new Dictionary<string, object>() {
                                { "code", (byte) NetworkServerResponses.KeepAlive }
                            } }
                        }));
                    }
                } catch (InvalidOperationException) {

                }
            }
        }

        public static void Send(Socket client, string message) {
            try {
                client.Send(Encoding.ASCII.GetBytes(message));
            } catch (ArgumentNullException argumentException) {
                Console.WriteLine("ArgumentNullException: " + argumentException.ToString());
            } catch (SocketException socketException) {
                Console.WriteLine("SocketException: " + socketException.ToString());
            } catch (Exception e) {
                Console.WriteLine(e.ToString());
            }
        }

        private static List<string> OpenPackage(byte[] package, int numBytes) {
            string content = Encoding.ASCII.GetString(package, 0, numBytes);
            List<string> openedPackage = new List<string>();
            foreach (string contentPart in content.Split("<$>")) {
                if (!string.IsNullOrWhiteSpace(contentPart)) {
                    openedPackage.Add(contentPart);
                }
            }
            return openedPackage;
        }

        public static void Send(Socket client, IList<ArraySegment<byte>> buffers) {
            try {
                client.Send(buffers);
            } catch (ArgumentNullException argumentException) {
                Console.WriteLine("Send -> ArgumentNullException: " + argumentException.ToString());
            } catch (SocketException socketException) {
                Console.WriteLine("Send -> SocketException: " + socketException.ToString());
            } catch (Exception e) {
                Console.WriteLine(e.ToString());
            }
        }

        public static IList<ArraySegment<byte>> CreatePackage(object[] objects) {
            IList<ArraySegment<byte>> package = new List<ArraySegment<byte>>();
            foreach (object objectToSend in objects) {
                package.Add(new ArraySegment<byte>(Encoding.ASCII.GetBytes(objectToSend.ToString())));
                package.Add(new ArraySegment<byte>(Encoding.ASCII.GetBytes("<$>")));
            }
            return package;
        }

        public static IList<ArraySegment<byte>> CreatePackage(Dictionary<string, object> data) {
            IList<ArraySegment<byte>> package = new List<ArraySegment<byte>>();
            foreach (KeyValuePair<string, object> pair in data) {
                package.Add(new ArraySegment<byte>(Encoding.ASCII.GetBytes(pair.Key)));
                package.Add(new ArraySegment<byte>(Encoding.ASCII.GetBytes("<#>")));
                package.Add(new ArraySegment<byte>(Encoding.ASCII.GetBytes(pair.Value.ToString())));
                package.Add(new ArraySegment<byte>(Encoding.ASCII.GetBytes("<$>")));
            }
            return package;
        }

        public static IList<ArraySegment<byte>> CreatePackage(List<Dictionary<string, object>> data) {
            IList<ArraySegment<byte>> package = new List<ArraySegment<byte>>();
            foreach (Dictionary<string, object> objectData in data) {
                foreach (KeyValuePair<string, object> pair in objectData) {
                    package.Add(new ArraySegment<byte>(Encoding.ASCII.GetBytes(pair.Key)));
                    package.Add(new ArraySegment<byte>(Encoding.ASCII.GetBytes("<#>")));
                    package.Add(new ArraySegment<byte>(Encoding.ASCII.GetBytes(pair.Value.ToString())));
                    package.Add(new ArraySegment<byte>(Encoding.ASCII.GetBytes("<$>")));
                }
                package.Add(new ArraySegment<byte>(Encoding.ASCII.GetBytes("<&>")));
            }
            return package;
        }

        private static IList<ArraySegment<byte>> CreateServerResponse(NetworkServerResponses response) {
            return CreatePackage(new Dictionary<string, object>() {
                { "code", (byte) response }
            });
        }
    }
}
