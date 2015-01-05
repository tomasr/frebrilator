using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.AspNet;
using Microsoft.Diagnostics.Tracing.Session;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Winterdom.Frebrilator {
  public class TraceCapture : IDisposable {
    private TraceEventSession session;

    public TraceCapture(String fileName) {
      var sessionName = Path.GetFileName(fileName);
      this.session = new TraceEventSession(sessionName, fileName);

      this.session.EnableProvider(Providers.AspNet_Trace, TraceEventLevel.Verbose, 0xF);
      this.session.EnableProvider(Providers.IIS_Trace, TraceEventLevel.Verbose, 0x4ffe);
      this.session.EnableProvider(Providers.IIS_IsapiTrace, TraceEventLevel.Verbose, 0);
      this.session.EnableProvider(Providers.ASP_Trace, TraceEventLevel.Verbose, 0);
    }

    public void Stop() {
      if ( this.session != null ) {
        this.session.Flush();
        this.session.Stop();
      }
    }

    public void Dispose() {
      Stop();
      if ( this.session != null ) {
        this.session.Dispose();
        this.session = null;
      }
    }
  }
}
