using Microsoft.Diagnostics.Tracing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Frebrilator.Tests {
  public static class TraceLoader {
    const String DefaultTrace = "freb.etl";

    public static ETWTraceEventSource Load(String traceFile = DefaultTrace) {
      return new ETWTraceEventSource(traceFile, TraceEventSourceType.FileOnly);
    }
  }
}
