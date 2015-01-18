using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.AspNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Winterdom.Frebrilator {
  public class FrebWriter {
    public const String EtwNs = "http://schemas.microsoft.com/win/2004/08/events/event";
    public const String EtwTraceNs = "http://schemas.microsoft.com/win/2004/08/events/trace";
    public const String FrebNs = "http://schemas.microsoft.com/win/2006/06/iis/freb";

    private XmlWriter writer;
    public String ComputerName { get; set; }
    private Encoding requestEncoding;
    private bool requestIsBinary;
    private Encoding responseEncoding;
    private bool responseIsBinary;

    public FrebWriter(XmlWriter xw) {
      this.writer = xw;
      this.requestEncoding = Encoding.UTF8;
      this.requestIsBinary = true;
      this.responseEncoding = Encoding.UTF8;
      this.responseIsBinary = true;
    }

    public void WriteFrebStream(Guid activityId, IEnumerable<TraceEvent> trace) {
      TraceEvent reqStart = trace.SingleOrDefault(t => IsRequestStart(t));
      TraceEvent reqEnd = trace.SingleOrDefault(t => IsRequestEnd(t));
      TraceEvent reqAuth = trace.SingleOrDefault(t => IsAuthSucceeded(t));
      if ( reqStart == null ) return; // can't write it, incomplete

      writer.WriteProcessingInstruction("xml-stylesheet", "type='text/xsl' href='freb.xsl'");
      writer.WriteComment(" saved from url=(0014)about:internet ");
      writer.WriteStartElement("failedRequest");
      writer.WriteAttributeString("url", reqStart.PayloadString(5));
      writer.WriteAttributeString("siteId", reqStart.PayloadString(1));
      writer.WriteAttributeString("appPoolId", reqStart.PayloadString(2));
      writer.WriteAttributeString("processId", ConvertValue(reqStart.ProcessID));
      writer.WriteAttributeString("verb", reqStart.PayloadString(6));

      writer.WriteAttributeString("remoteUserName", reqAuth != null ? reqAuth.PayloadString(3) : "");
      writer.WriteAttributeString("userName", reqAuth != null ? reqAuth.PayloadString(4) : "");
      // We just don't have this information in the trace
      writer.WriteAttributeString("tokenUserName", ""); 
      // Needs conversion
      writer.WriteAttributeString("authenticationType", reqAuth != null ? reqAuth.PayloadString(1) : "");

      writer.WriteAttributeString("activityId", ConvertValue(activityId));
      writer.WriteAttributeString("failureReason", "STATUS_CODE");

      writer.WriteAttributeString("statusCode", reqEnd != null ? ConvertValue(reqEnd.PayloadValue(3)) : "");
      writer.WriteAttributeString("triggerStatusCode", reqEnd != null ? ConvertValue(reqEnd.PayloadValue(3)) : "");
      writer.WriteAttributeString("timeTaken", reqEnd != null ? 
        ((long)(reqEnd.TimeStamp - reqStart.TimeStamp).TotalMilliseconds).ToString() : "");

      writer.WriteAttributeString("xmlns", "freb", "", FrebNs);
      ProcessEvents(activityId, trace);
      writer.WriteEndElement();
    }

    public void WriteEvent(Guid activityId, TraceEvent traceEvent) {
      writer.WriteStartElement("Event", EtwNs);
      WriteHeader(activityId, traceEvent);
      WriteEventData(traceEvent);
      WriteRenderingInfo(traceEvent);
      WriteExtendedTracingInfo(traceEvent);
      writer.WriteEndElement();
    }

    public void WriteHeader(Guid activityId, TraceEvent traceEvent) {
      writer.WriteStartElement("System", EtwNs);
      // <Provider Name="WWW Server" Guid="{3A2A4E84-4C21-4981-AE10-3FDA0D9B0F83}"/>
      writer.WriteStartElement("Provider", EtwNs);
      writer.WriteAttributeString("Name", MapProviderName(traceEvent.ProviderGuid));
      writer.WriteAttributeString("Guid", traceEvent.ProviderGuid.ToString("B"));
      writer.WriteEndElement();

      writer.WriteElementString("EventID", traceEvent.ID == TraceEventID.Illegal ? "0" : traceEvent.ID.ToString("d"));
      writer.WriteElementString("Version", traceEvent.Version.ToString());
      TraceEventLevel level = EventLevelMap.Resolve(traceEvent);
      writer.WriteElementString("Level", level.ToString("d"));
      writer.WriteElementString("Opcode", traceEvent.Opcode.ToString("d"));
      writer.WriteElementString("Keywords", AsHex((long)traceEvent.Keywords));
		  //<TimeCreated SystemTime="2014-12-07T21:49:33.268706800Z" />
      writer.WriteStartElement("TimeCreated", EtwNs);
      writer.WriteAttributeString("SystemTime", FormatDate(traceEvent.TimeStamp));
      writer.WriteEndElement();
		  // <Correlation ActivityID="{800000a9-0001-b100-b63f-84710c7967bb}" />
      writer.WriteStartElement("Correlation", EtwNs);
      writer.WriteAttributeString("ActivityID", activityId.ToString("B"));
      writer.WriteEndElement();
      // <Execution ProcessID="8588" ThreadID="5168" />
      writer.WriteStartElement("Execution", EtwNs);
      writer.WriteAttributeString("ProcessID", traceEvent.ProcessID.ToString());
      writer.WriteAttributeString("ThreadID", traceEvent.ThreadID.ToString());
      writer.WriteEndElement();
      writer.WriteElementString("Computer", EtwNs,
        String.IsNullOrEmpty(ComputerName) ?
        Environment.MachineName : ComputerName);

      writer.WriteEndElement();
    }

    public void WriteEventData(TraceEvent traceEvent) {
      writer.WriteStartElement("EventData", EtwNs);
      for ( int i=0; i < traceEvent.PayloadNames.Length; i++ ) {
        writer.WriteStartElement("Data", EtwNs);
        String name = traceEvent.PayloadNames[i];
        writer.WriteAttributeString("Name", name);
        object value = traceEvent.PayloadValue(i);
        if ( value is byte[] ) {
          WriteBufferContents(traceEvent, value);
        } else {
          writer.WriteString(ConvertValue(value));
        }
        writer.WriteEndElement();
      }
      writer.WriteEndElement();
    }

    public void WriteRenderingInfo(TraceEvent traceEvent) {
      writer.WriteStartElement("RenderingInfo", EtwNs);
      writer.WriteAttributeString("Culture", "en-US");
      writer.WriteElementString("Opcode", EtwNs, traceEvent.OpcodeName);

      if ( traceEvent.Keywords != TraceEventKeyword.None ) {
        writer.WriteStartElement("Keywords");
        writer.WriteEndElement();
      }
      TranslateFields(traceEvent);
      writer.WriteEndElement();
    }

    public void WriteExtendedTracingInfo(TraceEvent traceEvent) {
      // We cannot implement this properly at this time
      // because we cannot get at the TaskGuid.
      // Fortunately, this isn't used by the FREB xsl
      writer.WriteStartElement("ExtendedTracingInfo", EtwTraceNs);
      writer.WriteElementString("EventGuid", EtwTraceNs, traceEvent.TaskName);
      writer.WriteEndElement();
    }

    private void ProcessEvents(Guid activityId, IEnumerable<TraceEvent> trace) {
      foreach ( var e in trace ) {
        if ( IsRequestHeaders(e) ) {
          OnRequestHeaders(e);
        } else if ( IsResponseHeaders(e) ) {
          OnResponseHeaders(e);
        }
        WriteEvent(activityId, e);
      }
    }

    private void OnRequestHeaders(TraceEvent e) {
      String headers = e.PayloadString(1);
      // TODO: parse the headers properly
      if ( headers.Contains("Content-Type: text/") ) {
        this.requestIsBinary = false;
      } else if ( headers.Contains("charset=") ) {
        this.requestIsBinary = false;
      } else {
        this.requestIsBinary = true;
      }
      if ( headers.Contains("Content-Encoding: gzip") ) {
        this.requestIsBinary = true;
      }
    }
    private void OnResponseHeaders(TraceEvent e) {
      String headers = e.PayloadString(1);
      // TODO: parse the headers properly
      if ( headers.Contains("Content-Type: text/") ) {
        this.responseIsBinary = false;
      } else if ( headers.Contains("charset=") ) {
        this.responseIsBinary = false;
      } else {
        this.responseIsBinary = true;
      }
      if ( headers.Contains("Content-Encoding: gzip") ) {
        this.responseIsBinary = true;
      }
    }

    private void WriteBufferContents(TraceEvent traceEvent, object value) {
      byte[] data = value as byte[];
      bool isRequest = traceEvent.EventName.Contains("REQUEST");
      bool isBinary = false;
      Encoding encoding = Encoding.UTF8;
      if ( isRequest ) {
        isBinary = this.requestIsBinary;
        encoding = this.requestEncoding;
      } else {
        isBinary = this.responseIsBinary;
        encoding = this.responseEncoding;
      }
      // the buffer is null terminated, so always
      // skip the last byte
      String bufferContent =
        isBinary ? UrlEncode(data, 0, data.Length-1)
                 : encoding.GetString(data, 0, data.Length-1);

      this.writer.WriteString(bufferContent);
    }

    private String UrlEncode(byte[] data, int offset, int length) {
      StringBuilder buffer = new StringBuilder();
      for ( int i = offset; i < length; i++ ) {
        buffer.AppendFormat("%{0:x2}", data[i]);
      }
      return buffer.ToString();
    }

    private void TranslateFields(TraceEvent traceEvent) {
      String[] names = traceEvent.PayloadNames;
      for ( int i = 0; i < names.Length; i++ ) {
        if ( names[i] == "ErrorCode" ) {
          WriteExtraData(names[i], Native.FormatMessage((int)traceEvent.PayloadValue(i)));
          return;
        }
        object value = traceEvent.PayloadValue(i);
        if ( value != null && value.GetType().IsEnum ) {
          WriteExtraData(names[i], traceEvent.PayloadString(i));
        }
      }
    }

    private void WriteExtraData(String name, String value) {
      writer.WriteStartElement("freb", "Description", FrebNs);
      writer.WriteAttributeString("Data", name);
      writer.WriteString(value);
      writer.WriteEndElement();
    }

    private static String ConvertValue(object value) {
      if ( value == null ) return "";
      Type type = value.GetType();
      if ( type.IsEnum ) {
        return Convert.ToInt32(value).ToString();
      }
      if ( type == typeof(long) ) {
        // ugly workaround.... there are many ulong values in there
        return Convert.ToString((ulong)(long)value);
      }
      if ( type == typeof(DateTime) ) {
        return FormatDate((DateTime)value);
      }
      if ( type == typeof(Guid) ) {
        return ((Guid)value).ToString("B");
      }
      if ( type == typeof(bool) ) {
        return ((bool)value) ? "true" : "false";
      }
      return Convert.ToString(value);
    }

    private static String FormatDate(DateTime dateTime) {
      return dateTime.ToUniversalTime()
                     .ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
    }


    private static String AsHex(long value) {
      return String.Format("0x{0:x}", value);
    }

    public static bool IsRequestStart(TraceEvent e) {
      return e.ProviderGuid == Providers.IIS_Trace
          && e.OpcodeName == "GENERAL_REQUEST_START";
    }
    public static bool IsRequestEnd(TraceEvent e) {
      return e.ProviderGuid == Providers.IIS_Trace
          && e.OpcodeName == "GENERAL_REQUEST_END";
    }
    public static bool IsAuthSucceeded(TraceEvent e) {
      return e.ProviderGuid == Providers.IIS_Trace
          && e.OpcodeName == "AUTH_SUCCEEDED";
    }
    private static bool IsRequestHeaders(TraceEvent e) {
      return e.ProviderGuid == Providers.IIS_Trace
          && e.OpcodeName == "GENERAL_REQUEST_HEADERS";
    }
    private static bool IsResponseHeaders(TraceEvent e) {
      return e.ProviderGuid == Providers.IIS_Trace
          && e.OpcodeName == "GENERAL_RESPONSE_HEADERS";
    }

    public static String MapProviderName(Guid provider) {
      if ( provider == Providers.IIS_Trace ) {
        return "WWW Server";
      }
      if ( provider == Providers.IIS_IsapiTrace ) {
        return "ISAPI Extension";
      }
      if ( provider == Providers.ASP_Trace ) {
        return "ASP";
      }
      if ( provider == Providers.AspNet_Trace ) {
        return "ASPNET";
      }
      throw new InvalidOperationException("Unexpected ETW provider: " + provider);
    }
  }
}
