using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using System.IO;

namespace DOSTServer {
    static class Server {
        static void Main(string[] args) {
            try {
                Database.InitializeDatabase();
                EngineNetwork.InitializeServer();
            } catch (Exception ex) {
                Console.WriteLine("Error: " + ex.Message);
                Console.WriteLine("Press any key to finish...");
                Console.ReadLine();
            }
        }

        public static Dictionary<string, Dictionary<XName, string>> GetConfigFileElements() {
            string dir = AppDomain.CurrentDomain.BaseDirectory;
            if (!File.Exists(dir + "config.xml")) {
                new XDocument(
                    new XElement("Configuration",
                        new XElement(
                            "Database",
                            new XElement("Server", ""),
                            new XElement("DatabaseName", ""),
                            new XElement("DatabaseUser", ""),
                            new XElement("DatabasePassword", "")
                        ),
                        new XElement(
                            "Smtp",
                            new XElement("SMTPServer", "smtp.live.com"),
                            new XElement("Email", ""),
                            new XElement("EmailPassword", ""),
                            new XElement("Port", "587")
                        )
                    )
                ).Save(dir + "config.xml");
            }
            XDocument configXml = XDocument.Load(dir + "config.xml");
            Dictionary<string, Dictionary<XName, string>> xmlElements = new Dictionary<string, Dictionary<XName, string>>();
            foreach (var xmlElement in configXml.Root.Elements()) {
                var insideElement = xmlElement.Elements().ToDictionary(element => element.Name, element => element.Value);
                xmlElements.Add(xmlElement.Name.ToString(), insideElement);
            }
            foreach (KeyValuePair<string, Dictionary<XName, string>> xmlElement in xmlElements) {
                foreach (KeyValuePair<XName, string> insideElement in xmlElement.Value) {
                    if (insideElement.Key == "DatabasePassword") {
                        continue;
                    }
                    if (string.IsNullOrWhiteSpace(insideElement.Value)) {
                        Process.Start("notepad.exe", dir + "config.xml");
                        Environment.Exit(0);
                    }
                }
            }
            return xmlElements;
        }
    }
}
