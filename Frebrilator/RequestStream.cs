using Microsoft.Diagnostics.Tracing;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Winterdom.Frebrilator {
  public class RequestStream {
    public IReadOnlyList<TraceEvent> Events { get; private set; }

    public RequestStream(IList<TraceEvent> stream) {
      Events = new ReadOnlyCollection<TraceEvent>(stream);
    }
  }
}
