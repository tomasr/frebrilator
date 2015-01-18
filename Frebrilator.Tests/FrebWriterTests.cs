using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Winterdom.Frebrilator;
using Xunit;

namespace Frebrilator.Tests {
  public class FrebWriterTests {

    [Fact]
    public void WriteHeaderTest() {
      var trace = TraceLoader.LoadAllEvents();
      var e = trace[2];
      Guid activityId = Guid.NewGuid();

      StringWriter sw = new StringWriter();
      using ( XmlWriter xw = XmlWriter.Create(sw) ) {
        var writer = new FrebWriter(xw);
        writer.ComputerName = "DOMINION";
        writer.WriteHeader(activityId, e);
      }

      XDocument doc = XDocument.Parse(sw.ToString());
      XElement root = doc.Root;
      XNamespace freb = FrebWriter.EtwNs;
      Assert.Equal(freb + "System", root.Name);
      Assert.Equal("WWW Server", root.Element(freb+"Provider").Attribute("Name").Value);
      Assert.Equal("{3a2a4e84-4c21-4981-ae10-3fda0d9b0f83}", root.Element(freb + "Provider").Attribute("Guid").Value);
      Assert.Equal("0", root.Element(freb+"EventID").Value);
      Assert.Equal("1", root.Element(freb+"Version").Value);
      Assert.Equal("0", root.Element(freb+"Level").Value);
      Assert.Equal("1", root.Element(freb+"Opcode").Value);
      Assert.Equal("0x0", root.Element(freb+"Keywords").Value);
      Assert.Equal("2014-12-08T02:49:38.306Z", root.Element(freb + "TimeCreated").Attribute("SystemTime").Value);
      Assert.Equal(activityId.ToString("B"), root.Element(freb + "Correlation").Attribute("ActivityID").Value);

      var exec = root.Element(freb + "Execution");
      Assert.Equal(e.ProcessID.ToString(), exec.Attribute("ProcessID").Value);
      Assert.Equal(e.ThreadID.ToString(), exec.Attribute("ThreadID").Value);
      Assert.Equal("DOMINION", root.Element(freb+"Computer").Value);
    }

    [Fact]
    public void WriteEventDataTest() {
      var trace = TraceLoader.LoadAllEvents();
      var e = trace[2];
      Guid activityId = Guid.NewGuid();

      StringWriter sw = new StringWriter();
      using ( XmlWriter xw = XmlWriter.Create(sw) ) {
        new FrebWriter(xw).WriteEventData(e);
      }

      XDocument doc = XDocument.Parse(sw.ToString());
      XElement root = doc.Root;
      XNamespace freb = FrebWriter.EtwNs;

      Assert.Equal(freb + "EventData", root.Name);
      Assert.Equal("{800000a9-0001-b100-b63f-84710c7967bb}",
        FindDataElement(root, freb+"Data", "ContextId").Value);
      Assert.Equal("1",
        FindDataElement(root, freb+"Data", "SiteId").Value);
      Assert.Equal("DefaultAppPool",
        FindDataElement(root, freb+"Data", "AppPoolId").Value);
      Assert.Equal("12754194150618824872",
        FindDataElement(root, freb+"Data", "ConnId").Value);
      Assert.Equal("0",
        FindDataElement(root, freb+"Data", "RawConnId").Value);
      Assert.Equal("http://localhost:80/test1.aspx",
        FindDataElement(root, freb+"Data", "RequestURL").Value);
      Assert.Equal("GET",
        FindDataElement(root, freb+"Data", "RequestVerb").Value);
    }

    [Fact]
    public void WriteRenderingInfoTest() {
      var trace = TraceLoader.LoadAllEvents();
      var e = trace[2];
      Guid activityId = Guid.NewGuid();

      StringWriter sw = new StringWriter();
      using ( XmlWriter xw = XmlWriter.Create(sw) ) {
        new FrebWriter(xw).WriteRenderingInfo(e);
      }

      XDocument doc = XDocument.Parse(sw.ToString());
      XElement root = doc.Root;
      XNamespace freb = FrebWriter.EtwNs;

      Assert.Equal(freb + "RenderingInfo", root.Name);
      Assert.Equal("en-US", root.Attribute("Culture").Value);
      Assert.Equal("GENERAL_REQUEST_START", root.Element(freb + "Opcode").Value);
    }

    [Fact(Skip="No TaskGuid available")]
    public void WriteExtendedTracingInfoTest() {
      var trace = TraceLoader.LoadAllEvents();
      var e = trace[2];
      Guid activityId = Guid.NewGuid();

      StringWriter sw = new StringWriter();
      using ( XmlWriter xw = XmlWriter.Create(sw) ) {
        new FrebWriter(xw).WriteExtendedTracingInfo(e);
      }

      XDocument doc = XDocument.Parse(sw.ToString());
      XElement root = doc.Root;
      XNamespace freb = FrebWriter.EtwTraceNs;

      Assert.Equal(freb + "ExtendedTracingInfo", root.Name);
      // Won't pass at this time
      Assert.Equal("{D42CF7EF-DE92-473E-8B6C-621EA663113A}", root.Element(freb + "EventGuid").Value);
    }

    [Fact]
    public void WriteEventTest() {
      var trace = TraceLoader.LoadAllEvents();
      var e = trace[2];
      Guid activityId = Guid.NewGuid();

      StringWriter sw = new StringWriter();
      using ( XmlWriter xw = XmlWriter.Create(sw) ) {
        var writer = new FrebWriter(xw);
        writer.ComputerName = "DOMINION";
        writer.WriteEvent(activityId, e);
      }

      XDocument doc = XDocument.Parse(sw.ToString());
      XElement root = doc.Root;
      XNamespace freb = FrebWriter.EtwNs;

      Assert.Equal(freb + "Event", root.Name);
      Assert.Equal(4, root.Elements().Count());
    }

    private XElement FindDataElement(XElement root, XName name, String propName) {
      return root.Elements(name)
                 .Where(x => x.Attribute("Name").Value == propName)
                 .FirstOrDefault();
    }
  }
}
