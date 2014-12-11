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
        FrebWriter.WriteHeader(xw, activityId, e, "DOMINION");
      }

      XDocument doc = XDocument.Parse(sw.ToString());
      XElement root = doc.Root;
      XNamespace freb = FrebWriter.EtwNS;
      Assert.Equal(freb + "System", root.Name);
      Assert.Equal("WWW Server", root.Element(freb+"Provider").Attribute("Name").Value);
      Assert.Equal("{3a2a4e84-4c21-4981-ae10-3fda0d9b0f83}", root.Element(freb + "Provider").Attribute("Guid").Value);
      Assert.Equal("0", root.Element(freb+"EventID").Value);
      Assert.Equal("1", root.Element(freb+"Version").Value);
      Assert.Equal("0", root.Element(freb+"Level").Value);
      Assert.Equal("1", root.Element(freb+"OpCode").Value);
      Assert.Equal("0x0", root.Element(freb+"Keywords").Value);
      Assert.Equal("2014-12-08T02:49:38.306Z", root.Element(freb + "TimeCreated").Attribute("SystemTime").Value);
      Assert.Equal(activityId.ToString("B"), root.Element(freb + "Correlation").Attribute("ActivityID").Value);

      var exec = root.Element(freb + "Execution");
      Assert.Equal(e.ProcessID.ToString(), exec.Attribute("ProcessID").Value);
      Assert.Equal(e.ThreadID.ToString(), exec.Attribute("ThreadID").Value);
      Assert.Equal("DOMINION", root.Element(freb+"Computer").Value);

    }
  }
}
