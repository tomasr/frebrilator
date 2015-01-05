using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Winterdom.Frebrilator {
  public class StreamHandlerProvider : IStreamHandlerProvider {
    private IDictionary<Guid, EventStreamHandler> handlers;
    private String outputPath;

    public StreamHandlerProvider(String outputDirectory) {
      this.handlers = new Dictionary<Guid, EventStreamHandler>();
      this.outputPath = outputDirectory;
    }
    public IStreamHandler Get(Guid activityId) {
      EventStreamHandler handler;
      if ( !handlers.TryGetValue(activityId, out handler) ) {
        handler = new EventStreamHandler(activityId);
        handlers[activityId] = handler;
      }
      return handler;
    }

    public void Dispose() {
      foreach ( Guid activityId in handlers.Keys ) {
        WriteStream(activityId, handlers[activityId]);
      }
    }

    private void WriteStream(Guid activityId, EventStreamHandler handler) {
      if ( HasStart(handler) ) {
        XmlWriterSettings settings = new XmlWriterSettings();
        settings.Indent = true;
        settings.IndentChars = " ";
        settings.CheckCharacters = false;
        String path = Path.Combine(outputPath, String.Format("{0}.xml", activityId));
        using ( XmlWriter xw = XmlWriter.Create(path, settings) ) {
          FrebWriter.WriteFrebStream(xw, activityId, null, handler.Stream);
        }
      }
    }

    private bool HasStart(EventStreamHandler handler) {
      return handler.Stream.Any(x => FrebWriter.IsRequestStart(x)); 
    }
  }
}
