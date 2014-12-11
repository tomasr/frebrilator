using Microsoft.Diagnostics.Tracing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Winterdom.Frebrilator {
  public interface IStreamHandler {
    void AddEvent(TraceEvent traceEvent);
  }
}
