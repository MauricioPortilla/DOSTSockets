using System;
using System.Collections.Generic;
using System.Text;

namespace DOSTServer {
    static class PartidaNetwork {
        public static List<Dictionary<string, object>> GetGames() {
            List<Dictionary<string, object>> gamesData = new List<Dictionary<string, object>>() {
                { new Dictionary<string, object>() {
                    { "code", (byte) NetworkServerResponses.GamesList }
                } }
            };
            Database.ExecuteStoreQuery(
                "SELECT * FROM partida p WHERE ronda = 0 AND (" +
                    "SELECT COUNT(idjugador) AS numPlayers FROM jugador WHERE idpartida = p.idpartida" +
                ") < 4", null,
                (results) => {
                    foreach (var row in results) {
                        Dictionary<string, object> gameData = new Dictionary<string, object>();
                        foreach (var columnData in row.Columns) {
                            gameData.Add(columnData.Key, columnData.Value);
                        }
                        gamesData.Add(gameData);
                    }
                }
            );
            return gamesData;
        }

        public static List<Dictionary<string, object>> GetPlayersData(int idpartida) {
            List<Dictionary<string, object>> playersData = new List<Dictionary<string, object>>() {
                { new Dictionary<string, object>() {
                    { "code", (byte) NetworkServerResponses.GamePlayersList }
                } }
            };
            Database.ExecuteStoreQuery(
                "SELECT * FROM jugador WHERE idpartida = @idpartida",
                new Dictionary<string, object>() {
                    { "@idpartida", idpartida }
                }, (results) => {
                    foreach (var row in results) {
                        Dictionary<string, object> playerData = new Dictionary<string, object>();
                        foreach (var columnData in row.Columns) {
                            playerData.Add(columnData.Key, columnData.Value);
                        }
                        playersData.Add(playerData);
                    }
                }
            );
            return playersData;
        }
    }
}
