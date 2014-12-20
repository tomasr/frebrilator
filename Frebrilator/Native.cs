using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Winterdom.Frebrilator {
  public static class Native {
    public static String FormatMessage(int errorCode) {
      return new Win32Exception(errorCode).Message;
    }
  }
}
