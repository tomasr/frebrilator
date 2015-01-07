param($folder, $output)

function Find-Task($taskNodes, $taskName) {
  foreach ( $t in $taskNodes ) {
    if ( $t.name -eq $taskName ) {
      return $t
    }
  }
}
function Find-Opcode($task, $opcodeName) {
  foreach ( $o in $task.opcode ) {
    if ( $o.name -eq $opcodeName ) {
      return $o
    }
  }
}
function Guid-Literal($sguid) {
  $guid = new-object Guid $sguid
  $str = $guid.ToString('X');

  $result = "new Guid("
  $first = $true
  $parts = Select-String '0x[0-9a-f]+' -input $str -AllMatches | %{$_.Matches.Value}
  foreach ($v in $parts) {
    if (!$first) { $result += ', ' }
    $result += $v
    $first = $false
  }
  return $result + ')'
}

function Level-Literal($level) {
  switch ($level) {
    'win:Always' { 'TraceEventLevel.Always' }
    'win:Critical' { 'TraceEventLevel.Critical' }
    'win:Error' { 'TraceEventLevel.Error' }
    'win:Warning' { 'TraceEventLevel.Warning' }
    'win:Informational' { 'TraceEventLevel.Informational' }
    'win:Verbose' { 'TraceEventLevel.Verbose' }
    default { 'Unknown' }
  }
}


$allEvents = @()

get-childitem $folder -filter *.xml | ForEach-Object {
  $xml = [xml](Get-Content $_.FullName)

  $provider = (select-xml -XPath "//*[local-name()='provider']" -Xml $xml).Node.guid

  $taskNodes = $xml.SelectNodes("//*[local-name()='task']")

  $events = $xml.SelectNodes("//*[local-name()='event']")

  foreach ($e in $events) {
    $level = $e.level
    $opcodeName = $e.opcode
    $taskName = $e.task

    $task = Find-Task $taskNodes $taskName
    $opcode = Find-Opcode $task $opcodeName

    $obj = new-object PSObject -property @{
      'OpcodeName'=$opcodeName;
      'Opcode'=$opcode.value;
      'Task'=$task.eventGUID;
      'Level'=$level
    }
    $allEvents += $obj
  }
}

$text = @"
using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.Tracing;

namespace Winterdom.Frebrilator {
  public static class EventLevelMap {
    private static Dictionary<Tuple<Guid,int>, TraceEventLevel> map = 
      new Dictionary<Tuple<Guid,int>, TraceEventLevel>();

    private static void Add(Guid task, int opcode, TraceEventLevel level) {
      map.Add(new Tuple<Guid,int>(task, opcode), level);
    }

    public static TraceEventLevel Resolve(TraceEvent e) {
      return map[new Tuple<Guid, int>(e.ProviderGuid, (int)e.Opcode)];
    }

    static EventLevelMap() {
"@

$allEvents | %{
  $text += "`r`n"
  $text += "      Add(" + (Guid-Literal $_.Task) + ", $($_.Opcode), " + (Level-Literal $_.Level) + ");"
}

$text += "`r`n"
$text += @"
    }
  }
}
"@

Set-Content -Path $output -Value $text
