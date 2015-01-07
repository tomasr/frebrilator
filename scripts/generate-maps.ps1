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
      'OpcodeName'=$opcode.message;
      'Opcode'=$opcode.value;
      'Task'=$task.eventGUID;
      'TaskName'=$task.name;
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
    private static Dictionary<String, TraceEventLevel> map = 
      new Dictionary<String, TraceEventLevel>();

    private static void Add(String eventName, int opcode, TraceEventLevel level) {
      map.Add(eventName+opcode, level);
    }

    public static TraceEventLevel Resolve(TraceEvent e) {
      return map[e.EventName+(int)e.Opcode];
    }

    static EventLevelMap() {
"@

$allEvents | %{
  $text += "`r`n"
  $text += '      Add("' + $_.TaskName +  '/' + $_.OpcodeName + '", ' + $_.Opcode + ', ' + (Level-Literal $_.Level) + ");"
}

$text += "`r`n"
$text += @"
    }
  }
}
"@

Set-Content -Path $output -Value $text
