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

    private IList<IDisposable> subscriptions;
    private IStreamHandlerProvider handlerProvider;
    private String computerName;

    public EventAggregator(IStreamHandlerProvider provider) {
      this.handlerProvider = provider;
      this.subscriptions = new List<IDisposable>();
    }

    public void Start(ETWTraceEventSource eventSource) {
      if ( this.subscriptions.Count > 0 ) {
        throw new InvalidOperationException("Observation already started");
      }
      this.subscriptions.Clear();

      this.computerName = Environment.MachineName.ToUpper();
      TraceEventParser parser = new AspNetTraceEventParser(eventSource);
      this.subscriptions.Add(parser.ObserveAll().Subscribe(this));
      //*
      parser = new IIS_TraceTraceEventParser(eventSource);
      this.subscriptions.Add(parser.ObserveAll().Subscribe(this));
      parser = new ASP_TraceTraceEventParser(eventSource);
      this.subscriptions.Add(parser.ObserveAll().Subscribe(this));
      parser = new IIS_IsapiTraceTraceEventParser(eventSource);
      this.subscriptions.Add(parser.ObserveAll().Subscribe(this));
      //*/
      /*
      parser = new RegisteredTraceEventParser(eventSource);
      this.subscriptions.Add(parser.ObserveAll().Subscribe(this));
      //*/
    }

    public void Dispose() {
      if ( this.subscriptions.Count > 0 ) {
        foreach ( var e in this.subscriptions ) {
          e.Dispose();
        }
        this.subscriptions.Clear();
      }
    }

    void IObserver<TraceEvent>.OnNext(TraceEvent obj) {
      if ( obj.ProviderGuid == KernelTraceEventParser.ProviderGuid ) {
        if ( obj.EventName == "SystemConfig/CPU" ) {
          this.computerName = (String)obj.PayloadByName("ComputerName");
        }
      }
      bool include = obj.ProviderGuid == Providers.IIS_Trace
                  || obj.ProviderGuid == Providers.IIS_IsapiTrace
                  || obj.ProviderGuid == Providers.ASP_Trace
                  || obj.ProviderGuid == Providers.AspNet_Trace;
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
