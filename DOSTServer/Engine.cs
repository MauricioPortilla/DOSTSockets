using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace DOSTServer {
    class Engine {
        public static string HashWithSHA256(string text) {
            return string.Join("", (
                SHA256.Create().ComputeHash(
                    Encoding.UTF8.GetBytes(text)
                )
            ).Select(x => x.ToString("x2")).ToArray()).ToLower();
        }
    }
}
