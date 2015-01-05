using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Winterdom.Frebrilator {
  public static class Providers {
    public static readonly Guid IIS_Trace = new Guid("{3a2a4e84-4c21-4981-ae10-3fda0d9b0f83}");
    public static readonly Guid ASP_Trace = new Guid("{06b94d9a-b15e-456e-a4ef-37c984a2cb4b}");
    public static readonly Guid IIS_IsapiTrace = new Guid("{a1c2040e-8840-4c31-ba11-9871031a19ea}");
    public static readonly Guid AspNet_Trace = new Guid("{AFF081FE-0247-4275-9C4E-021F3DC1DA35}");
  }
}
