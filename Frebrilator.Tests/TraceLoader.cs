using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
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

    public static IList<TraceEvent> LoadAllEvents(String traceFile = DefaultTrace) {
      using ( var source = Load(traceFile) ) {
        RegisteredTraceEventParser parser = new RegisteredTraceEventParser(source);
        List<TraceEvent> list = new List<TraceEvent>();
        parser.All += (e) => {
          list.Add(e.Clone());
        };
        source.Process();
        return list;
      }
    }
  }
}
