using Microsoft.Diagnostics.Tracing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Winterdom.Frebrilator {
  public class EventAggregator : IObserver<TraceEvent>, IDisposable {

    private IDisposable subscription;

    public void Start(IObservable<TraceEvent> events) {
      if ( this.subscription != null ) {
        throw new InvalidOperationException("Observation already started");
      }
      this.subscription = events.Subscribe(this);
    }

    public IEnumerable<RequestStream> AllRequests() {
      throw new NotImplementedException();
    }

    public void Dispose() {
      if ( this.subscription != null ) {
        this.subscription.Dispose();
        this.subscription = null;
      }
    }

    void IObserver<TraceEvent>.OnCompleted() {
    }

    void IObserver<TraceEvent>.OnError(Exception error) {
    }

    void IObserver<TraceEvent>.OnNext(TraceEvent value) {
    }
  }
}
