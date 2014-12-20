using Microsoft.Diagnostics.Tracing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Winterdom.Frebrilator {
  class Program {
    public CommandAction Action { get; set; }
    public String EtlFile { get; set; }
    public String OutputDir { get; set; }

    static void Main(string[] args) {
      Program p = new Program();
      if ( !p.ParseArgs(args) ) {
        Usage();
        return;
      }
      if ( !p.VerifyArgs() ) {
        Usage();
        return;
      }

      p.Run();

    }

    private void Run() {
      switch ( Action ) {
        case CommandAction.Capture:
          CaptureTrace();
          break;
        case CommandAction.Process:
          ProcessTrace();
          break;
      }
    }

    private void ProcessTrace() {
      using ( ETWTraceEventSource source = new ETWTraceEventSource(EtlFile) ) {
        using ( var provider = new StreamHandlerProvider(OutputDir) )
        using ( var aggregator = new EventAggregator(provider) ) {
          aggregator.Start(source);
          source.Process();
        }
      }
    }

    private void CaptureTrace() {
      using ( TraceCapture capture = new TraceCapture(EtlFile) ) {
        Console.WriteLine("Press enter to stop capture");
        Console.ReadLine();
      }
    }

    private bool VerifyArgs() {
      if ( Action == CommandAction.Process && !File.Exists(EtlFile) ) {
        Console.WriteLine("Input file not found!");
        return false;
      }
      if ( !String.IsNullOrEmpty(OutputDir) && !Directory.Exists(OutputDir) ) {
        Console.WriteLine("Output directory not found!");
        return false;
      }
      return true;
    }

    private bool ParseArgs(String[] args) {
      if ( args.Length < 1 )
        return false;
      String cmd = args[0].ToLower();
      if ( cmd == "-c" ) {
        this.Action = CommandAction.Capture;

        if ( args.Length < 2 )
          return false;

        this.EtlFile = args[1];
        return true;
      } else if ( cmd == "-p" ) {
        this.Action = CommandAction.Process;

        if ( args.Length < 3 )
          return false;

        this.EtlFile = args[1];
        this.OutputDir = args[2];
        return true;
      }
      return false;
    }

    private static void Usage() {
      Console.WriteLine("To capture a new ETW trace:");
      Console.WriteLine("  frebrilator -c <etlfile>");
      Console.WriteLine();
      Console.WriteLine("To process an existing ETW trace:");
      Console.WriteLine("  frebrilator -p <etlfile> <output_dir>");
    }
  }


  enum CommandAction {
    Capture,
    Process
  }
}
