using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;

namespace DOST {
    enum NetworkClientRequests { // 0 a 50
        Logout = 0x00,
        Login = 0x01,
        Register = 0x02,
        GetGames = 0x03,
        GetAccountData = 0x04,
        GetGamePlayers = 0x05
    }
    enum NetworkServerResponses { // 50 en adelante
        LoginError = 0x33,
        RegisterError = 0x34,
        RegisterSuccess = 0x35,
        AccountNotConfirmed = 0x36
    }

    class EngineNetwork {
        private static Socket sender;
        public static void EstablishConnection() {
            try {
                IPHostEntry ipHost = Dns.GetHostEntry(Dns.GetHostName());
                IPAddress ipAddr = ipHost.AddressList[0];
                IPEndPoint localEndPoint = new IPEndPoint(ipAddr, 25618);
                sender = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                try {
                    sender.Connect(localEndPoint);
                    Console.WriteLine("EstablishConnection -> Socket connected to: " + sender.RemoteEndPoint.ToString());
                } catch (ArgumentNullException argumentException) {
                    Console.WriteLine("EstablishConnection -> ArgumentNullException: " + argumentException.ToString());
                } catch (SocketException socketException) {
                    Console.WriteLine("EstablishConnection -> SocketException: " + socketException.ToString());
                } catch (Exception e) {
                    Console.WriteLine("EstablishConnection -> Unexpected exception: " + e.ToString());
                }
            } catch (Exception e) {
                Console.WriteLine(e.ToString());
            }
        }

        public static void Send(string message) {
            try {
                sender.Send(Encoding.ASCII.GetBytes(message));
            } catch (ArgumentNullException argumentException) {
                Console.WriteLine("Send -> ArgumentNullException: " + argumentException.ToString());
            } catch (SocketException socketException) {
                Console.WriteLine("Send -> SocketException: " + socketException.ToString());
            } catch (Exception e) {
                Console.WriteLine(e.ToString());
            }
        }

        public static void Send(IList<ArraySegment<byte>> buffers) {
            try {
                sender.Send(buffers);
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

        private static List<string> OpenPackage(byte[] package, int numBytes) {
            // data1<$>data2<$>
            string content = Encoding.ASCII.GetString(package, 0, numBytes);
            List<string> openedPackage = new List<string>();
            foreach (string contentPart in content.Split("<$>".ToCharArray())) {
                if (!string.IsNullOrWhiteSpace(contentPart)) {
                    openedPackage.Add(contentPart);
                }
            }
            return openedPackage;
        }

        private static Dictionary<string, string> OpenPackageAsDictionary(byte[] package, int numBytes) {
            // usuario<#>Frey<$>password<#>value<$>
            string content = Encoding.ASCII.GetString(package, 0, numBytes);
            Dictionary<string, string> openedPackage = new Dictionary<string, string>();
            string[] packageSegment = Regex.Split(content, @"(<\$>)");
            foreach (string contentPart in packageSegment) {
                if (!string.IsNullOrWhiteSpace(contentPart)) {
                    string[] contentPairKeyValue = Regex.Split(contentPart, @"(<#>)");
                    if (contentPairKeyValue.Length < 2) {
                        continue;
                    }
                    openedPackage.Add(contentPairKeyValue[0], contentPairKeyValue[2]);
                }
            }
            return openedPackage;
        }

        private static List<Dictionary<string, string>> OpenPackageAsMultipleData(byte[] package, int numBytes) {
            // usuario<#>Frey<$>password<#>value<$><&>usuario<#>Freya<$>password<#>value2<$><&>
            string content = Encoding.ASCII.GetString(package, 0, numBytes);
            List<Dictionary<string, string>> openedPackage = new List<Dictionary<string, string>>();
            List<string> packageSegment = Regex.Split(content, @"(<&>)").ToList();
            packageSegment.RemoveAll(x => x == "<&>");
            foreach (string dataCollection in packageSegment) {
                if (string.IsNullOrWhiteSpace(dataCollection) || dataCollection == "<&>") {
                    continue;
                }
                Dictionary<string, string> dataCollectionPackage = new Dictionary<string, string>();
                List<string> segment = Regex.Split(dataCollection, @"(<\$>)").ToList();
                segment.RemoveAll(x => x == "<$>");
                foreach (string dataPart in segment) {
                    if (!string.IsNullOrWhiteSpace(dataPart)) {
                        List<string> contentPairKeyValue = Regex.Split(dataPart, @"(<#>)").ToList();
                        contentPairKeyValue.RemoveAll(x => x == "<#>");
                        if (contentPairKeyValue.Count < 2) {
                            continue;
                        }
                        dataCollectionPackage.Add(contentPairKeyValue[0], contentPairKeyValue[1]);
                    }
                }
                openedPackage.Add(dataCollectionPackage);
            }
            return openedPackage;
        }

        public static List<string> Receive() {
            byte[] buffer = new byte[1024];
            while (true) {
                int bytesReceived = sender.Receive(buffer);
                if (bytesReceived > 0) {
                    return OpenPackage(buffer, bytesReceived);
                }
            }
        }

        public static Dictionary<string, string> ReceiveAsDictionary() {
            byte[] buffer = new byte[1024];
            while (true) {
                int bytesReceived = sender.Receive(buffer);
                if (bytesReceived > 0) {
                    return OpenPackageAsDictionary(buffer, bytesReceived);
                }
            }
        }

        public static List<Dictionary<string, string>> ReceiveMultipleData() {
            byte[] buffer = new byte[1024];
            while (true) {
                int bytesReceived = sender.Receive(buffer);
                if (bytesReceived > 0) {
                    return OpenPackageAsMultipleData(buffer, bytesReceived);
                }
            }
        }
    }
}
