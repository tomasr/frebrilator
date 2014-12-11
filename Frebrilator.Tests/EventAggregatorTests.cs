using Microsoft.Diagnostics.Tracing;
using System;
using System.Collections.Generic;
using System.Linq;
using Winterdom.Frebrilator;
using Xunit;

namespace Frebrilator.Tests {
  public class EventAggregatorTests {
    [Fact]
    public void AggregatesByActivityId() {
      var provider = new CountingHandlerProvider();
      var source = TraceLoader.Load();
      using ( var aggregator = new EventAggregator(provider) ) {
        aggregator.Start(source);
        source.Process();

        Assert.Equal(7, provider.Count);
      }
    }

    class CountingHandlerProvider : IStreamHandlerProvider {
      private Dictionary<Guid, IStreamHandler> handlers = new Dictionary<Guid, IStreamHandler>();
      public int Count {
        get { return handlers.Count; }
      }
      public IStreamHandler Get(Guid activityId) {
        IStreamHandler result;
        if ( !handlers.TryGetValue(activityId, out result) ) {
          result = new CountingHandler();
          handlers[activityId] = result;
        }
        return result;
      }

      public void Dispose() {
      }
    }

    class CountingHandler : IStreamHandler {
      public void AddEvent(TraceEvent traceEvent) {
      }
    }
  }
}
