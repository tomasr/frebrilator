using Microsoft.Diagnostics.Tracing;
using System;
using System.Linq;
using Winterdom.Frebrilator;
using Xunit;

namespace Frebrilator.Tests {
  public class EventAggregatorTests {
    [Fact]
    public void AggregatesByActivityId() {
      var source = TraceLoader.Load();
      using ( var aggregator = new EventAggregator() ) {
        var observable = source.ObserveAll();
        aggregator.Start(observable);

        source.Process();

        Assert.Equal(4, aggregator.AllRequests().Count());
      }
    }
  }
}
