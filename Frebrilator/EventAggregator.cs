using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.AspNet;
using Microsoft.Diagnostics.Tracing.Session;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Winterdom.Frebrilator {
  public class EventAggregator : IObserver<TraceEvent>, IDisposable {

    private IDisposable subscription;
    private IStreamHandlerProvider handlerProvider;
    private String computerName;

    public EventAggregator(IStreamHandlerProvider provider) {
      this.handlerProvider = provider;
    }

    public void Start(ETWTraceEventSource eventSource) {
      if ( this.subscription != null ) {
        throw new InvalidOperationException("Observation already started");
      }

      this.computerName = Environment.MachineName.ToUpper();
      var parser = new RegisteredTraceEventParser(eventSource);
      this.subscription = parser.ObserveAll().Subscribe(this);
    }

    public void Dispose() {
      if ( this.subscription != null ) {
        this.subscription.Dispose();
        this.subscription = null;
      }
    }

    void IObserver<TraceEvent>.OnNext(TraceEvent obj) {
      if ( obj.ProviderGuid == KernelTraceEventParser.ProviderGuid ) {
        if ( obj.EventName == "SystemConfig/CPU" ) {
          this.computerName = (String)obj.PayloadByName("ComputerName");
        }
      }
      bool include = obj.ProviderGuid == IIS_TraceTraceEventParser.ProviderGuid
                  || obj.ProviderGuid == IIS_IsapiTraceTraceEventParser.ProviderGuid
                  || obj.ProviderGuid == ASP_TraceTraceEventParser.ProviderGuid
                  || obj.ProviderGuid == AspNetTraceEventParser.ProviderGuid;
      if ( include ) {
        // All entries logged into FREB traces
        // will contain a ContextId value that links all related
        // events together.
        // Normally TraceEvent.ActivityId would do this
        // but that will always be Guid.Empty because
        // these are "classic" providers
        Guid activityId = (Guid)obj.PayloadByName("ContextId");
        var handler = this.handlerProvider.Get(activityId);
        handler.AddEvent(obj);
      }
    }

    void IObserver<TraceEvent>.OnCompleted() {
    }

    void IObserver<TraceEvent>.OnError(Exception error) {
    }

  }
}
