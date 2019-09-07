using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOST {
    class Session {
        private static Cuenta cuenta;
        public static Cuenta Cuenta {
            get {
                return cuenta;
            }
            set {
                cuenta = value;
            }
        }
    }
}
