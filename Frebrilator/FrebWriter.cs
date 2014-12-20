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
  public static class FrebWriter {
    public const String EtwNs = "http://schemas.microsoft.com/win/2004/08/events/event";
    public const String EtwTraceNs = "http://schemas.microsoft.com/win/2004/08/events/trace";
    public const String FrebNs = "http://schemas.microsoft.com/win/2006/06/iis/freb";

    public static void WriteFrebStream(XmlWriter xw, Guid activityId, String computerName, IEnumerable<TraceEvent> trace) {
      TraceEvent reqStart = trace.SingleOrDefault(t => IsRequestStart(t));
      TraceEvent reqEnd = trace.SingleOrDefault(t => IsRequestEnd(t));
      TraceEvent reqAuth = trace.SingleOrDefault(t => IsAuthSucceeded(t));
      if ( reqStart == null ) return; // can't write it, incomplete

      xw.WriteProcessingInstruction("xml-stylesheet", "type='text/xsl' href='freb.xsl'");
      xw.WriteStartElement("failedRequest");
      xw.WriteAttributeString("url", reqStart.PayloadString(5));
      xw.WriteAttributeString("siteId", reqStart.PayloadString(1));
      xw.WriteAttributeString("appPoolId", reqStart.PayloadString(2));
      xw.WriteAttributeString("processId", ConvertValue(reqStart.ProcessID));
      xw.WriteAttributeString("verb", reqStart.PayloadString(6));

      xw.WriteAttributeString("remoteUserName", reqAuth != null ? reqAuth.PayloadString(3) : "");
      xw.WriteAttributeString("userName", reqAuth != null ? reqAuth.PayloadString(4) : "");
      // We just don't have this information in the trace
      xw.WriteAttributeString("tokenUserName", ""); 
      // Needs conversion
      xw.WriteAttributeString("authenticationType", reqAuth != null ? reqAuth.PayloadString(5) : "");

      xw.WriteAttributeString("activityId", ConvertValue(activityId));
      xw.WriteAttributeString("failureReason", "STATUS_CODE");

      xw.WriteAttributeString("statusCode", reqEnd != null ? ConvertValue(reqEnd.PayloadValue(3)) : "");
      xw.WriteAttributeString("triggerStatusCode", reqEnd != null ? ConvertValue(reqEnd.PayloadValue(3)) : "");
      xw.WriteAttributeString("timeTaken", reqEnd != null ? 
        ((long)(reqEnd.TimeStamp - reqStart.TimeStamp).TotalMilliseconds).ToString() : "");

      xw.WriteAttributeString("xmlns", "freb", "", FrebNs);
      foreach ( var e in trace ) {
        WriteEvent(xw, activityId, e, computerName);
      }
      xw.WriteEndElement();
    }

    public static void WriteEvent(XmlWriter xw, Guid activityId, TraceEvent traceEvent, String computerName) {
      xw.WriteStartElement("Event", EtwNs);
      WriteHeader(xw, activityId, traceEvent, computerName);
      WriteEventData(xw, traceEvent);
      WriteRenderingInfo(xw, traceEvent);
      WriteExtendedTracingInfo(xw, traceEvent);
      xw.WriteEndElement();
    }

    public static void WriteHeader(XmlWriter xw, Guid activityId, TraceEvent traceEvent, String computerName) {
      xw.WriteStartElement("System", EtwNs);
      // <Provider Name="WWW Server" Guid="{3A2A4E84-4C21-4981-AE10-3FDA0D9B0F83}"/>
      xw.WriteStartElement("Provider", EtwNs);
      xw.WriteAttributeString("Name", MapProviderName(traceEvent.ProviderGuid));
      xw.WriteAttributeString("Guid", traceEvent.ProviderGuid.ToString("B"));
      xw.WriteEndElement();

      xw.WriteElementString("EventID", traceEvent.ID == TraceEventID.Illegal ? "0" : traceEvent.ID.ToString("d"));
      xw.WriteElementString("Version", traceEvent.Version.ToString());
      xw.WriteElementString("Level", traceEvent.Level.ToString("d"));
      xw.WriteElementString("OpCode", traceEvent.Opcode.ToString("d"));
      xw.WriteElementString("Keywords", AsHex((long)traceEvent.Keywords));
		  //<TimeCreated SystemTime="2014-12-07T21:49:33.268706800Z" />
      xw.WriteStartElement("TimeCreated", EtwNs);
      xw.WriteAttributeString("SystemTime", FormatDate(traceEvent.TimeStamp));
      xw.WriteEndElement();
		  // <Correlation ActivityID="{800000a9-0001-b100-b63f-84710c7967bb}" />
      xw.WriteStartElement("Correlation", EtwNs);
      xw.WriteAttributeString("ActivityID", activityId.ToString("B"));
      xw.WriteEndElement();
      // <Execution ProcessID="8588" ThreadID="5168" />
      xw.WriteStartElement("Execution", EtwNs);
      xw.WriteAttributeString("ProcessID", traceEvent.ProcessID.ToString());
      xw.WriteAttributeString("ThreadID", traceEvent.ThreadID.ToString());
      xw.WriteEndElement();
      xw.WriteElementString("Computer", EtwNs, String.IsNullOrEmpty(computerName) ? Environment.MachineName : computerName);

      xw.WriteEndElement();
    }

    public static void WriteEventData(XmlWriter xw, TraceEvent traceEvent) {
      xw.WriteStartElement("EventData", EtwNs);
      for ( int i=0; i < traceEvent.PayloadNames.Length; i++ ) {
        xw.WriteStartElement("Data", EtwNs);
        xw.WriteAttributeString("Name", traceEvent.PayloadNames[i]);
        xw.WriteString(ConvertValue(traceEvent.PayloadValue(i)));
        xw.WriteEndElement();
      }
      xw.WriteEndElement();
    }
    public static void WriteRenderingInfo(XmlWriter xw, TraceEvent traceEvent) {
      xw.WriteStartElement("RenderingInfo", EtwNs);
      xw.WriteAttributeString("Culture", "en-US");
      xw.WriteElementString("OpCode", EtwNs, traceEvent.OpcodeName);

      if ( traceEvent.Keywords != TraceEventKeyword.None ) {
        xw.WriteStartElement("Keywords");
        xw.WriteEndElement();
      }
      TranslateFields(xw, traceEvent);
      xw.WriteEndElement();
    }

    public static void WriteExtendedTracingInfo(XmlWriter xw, TraceEvent traceEvent) {
      // We cannot implement this properly at this time
      // because we cannot get at the TaskGuid.
      // Fortunately, this isn't used by the FREB xsl
      xw.WriteStartElement("ExtendedTracingInfo", EtwTraceNs);
      xw.WriteElementString("EventGuid", EtwTraceNs, traceEvent.TaskName);
      xw.WriteEndElement();
    }

    private static void TranslateFields(XmlWriter xw, TraceEvent traceEvent) {
      String[] names = traceEvent.PayloadNames;
      for ( int i = 0; i < names.Length; i++ ) {
        if ( names[i] == "ErrorCode" ) {
          WriteExtraData(xw, names[i], Native.FormatMessage((int)traceEvent.PayloadValue(i)));
        } else if ( names[i] == "NotificationStatus" ) {
          WriteExtraData(xw, names[i], traceEvent.PayloadString(i));
        } else if ( names[i] == "Notification" ) {
          WriteExtraData(xw, names[i], traceEvent.PayloadString(i));
        } else if ( names[i] == "CachePolicy" ) {
          WriteExtraData(xw, names[i], traceEvent.PayloadString(i));
        } else if ( names[i] == "Result" ) {
          WriteExtraData(xw, names[i], traceEvent.PayloadString(i));
        }
      }
    }

    private static void WriteExtraData(XmlWriter xw, String name, String value) {
      xw.WriteStartElement("freb", "Description", FrebNs);
      xw.WriteAttributeString("Data", name);
      xw.WriteString(value);
      xw.WriteEndElement();
    }

    private static String ConvertValue(object value) {
      if ( value == null ) return "";
      Type type = value.GetType();
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
      return Convert.ToString(value);
    }

    private static String FormatDate(DateTime dateTime) {
      return dateTime.ToUniversalTime()
                     .ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
    }


    private static string AsHex(long value) {
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
