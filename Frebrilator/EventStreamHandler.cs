using Microsoft.Diagnostics.Tracing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Winterdom.Frebrilator {
  public class EventStreamHandler : IStreamHandler {
    private IList<TraceEvent> stream;
    public Guid ActivityId { get; private set; }
    public IEnumerable<TraceEvent> Stream {
      get { return stream; }
    }
    
    public EventStreamHandler(Guid activityId) {
      this.ActivityId = activityId;
      this.stream = new List<TraceEvent>();
    }

    public void AddEvent(TraceEvent traceEvent) {
      this.stream.Add(traceEvent.Clone());
    }
  }
}
