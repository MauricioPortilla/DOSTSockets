using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Data.SqlClient;
using System.Xml.Linq;
using System.Diagnostics;

namespace DOSTServer {
    static class Database {
        private static SqlConnection sql;

        public static void InitializeDatabase() {
            var xmlElements = Server.GetConfigFileElements();
            sql = new SqlConnection(
                @"Server=" + xmlElements["Database"]["Server"] +
                ";Database=" + xmlElements["Database"]["DatabaseName"] +
                ";Trusted_Connection=False;User ID=" + xmlElements["Database"]["DatabaseUser"] +
                ";Password=" + xmlElements["Database"]["DatabasePassword"]
            );
            try {
                sql.Open();
            } catch (SqlException sqlException) {
                Console.WriteLine("Cannot connect to database.\nError: " + sqlException.Message);
                throw;
            } finally {
                if (sql.State == System.Data.ConnectionState.Open) {
                    sql.Close();
                }
            }
        }

		public static void ExecuteQuery(string query) {
			sql.Open();
			SqlDataAdapter command = new SqlDataAdapter {
				InsertCommand = new SqlCommand(query) {
					Connection = sql
				}
			};
			command.InsertCommand.ExecuteNonQuery();
			sql.Close();
		}

		public static bool ExecuteUpdate(string query, Dictionary<string, object> args, Action<Exception> OnFail = null) {
			try {
				sql.Open();
				SqlDataAdapter cmd = new SqlDataAdapter {
					InsertCommand = new SqlCommand(query, sql)
				};
				if (args != null) {
					foreach (var arg in args) {
                        Console.WriteLine("Arg Complex: " + arg.Key + " | Val: " + arg.Value);
                        cmd.InsertCommand.Parameters.AddWithValue(arg.Key, arg.Value);
					}
				}
				cmd.InsertCommand.ExecuteNonQuery();
				sql.Close();
                return true;
			} catch (Exception e) {
				if(OnFail != null) {
					OnFail.Invoke(e);
				}
				Console.WriteLine("EXCEPTION ERROR CQ -> " + e.Message.ToString());
                return false;
			}
		}

		public static List<DatabaseStruct.SSQLRow> ExecuteStoreQuery(
			string query, Dictionary<string, object> args, 
			Action<List<DatabaseStruct.SSQLRow>> action, Func<bool> zeroResults = null
		) {
			try {
				sql.Open();
				List<DatabaseStruct.SSQLRow> result = new List<DatabaseStruct.SSQLRow>();
				SqlCommand cmd = new SqlCommand(query, sql);
				if (args != null) {
					foreach (var arg in args) {
						// Console.WriteLine("Arg: " + arg.Key + " | Val: " + arg.Value);
						cmd.Parameters.AddWithValue(arg.Key, arg.Value);
					}
				}
				using (SqlDataReader reader = cmd.ExecuteReader()) {
					var read = reader.Read();
					while (read) {
						DatabaseStruct.SSQLRow s = new DatabaseStruct.SSQLRow(new Dictionary<string, object>());
						for (int i = 0; i < reader.FieldCount; i++) {
							try {
								s.Columns.Add(reader.GetName(i), reader.GetValue(i));
							} catch (IndexOutOfRangeException) {
								Console.WriteLine("Catch Exception");
								break;
							}
						}
						result.Add(s);
						read = reader.Read();
					}
				}
				sql.Close();
				if (result.Count > 0) {
					if (action != null)
						action.Invoke(result);
				} else {
					if (zeroResults != null)
						zeroResults.Invoke();
				}
				return result;
			} catch (Exception e) {
				if(sql.State == System.Data.ConnectionState.Open) {
					sql.Close();
				}
                Console.WriteLine("EXCEPTION ERROR SQ -> " + e.Message.ToString());
				return new List<DatabaseStruct.SSQLRow>();
            }
        }
	}
}
