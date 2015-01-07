param($filename)

$xml = [xml](gc $filename)

$events = $xml.SelectNodes("//*[local-name()='event']")

$opcodeValues = @()
$opcodeNames = @()
$opcodeMsgs = @()
$opcodeTasks = @()


foreach ($e in $events) {
  $opcode = $e.opcode
  $task = $e.task
  $e.SetAttribute('opcode', "$task$opcode")
  $opcodeValues += $opcode
  $opcodeNames += "$task$opcode"
  $opcodeMsgs += $e.template
  $opcodeTasks += $task
}


for ($i = 0; $i -lt $opcodeValues.Count; $i++) {
  $opcode = $opcodeValues[$i]
  $name = $opcodeNames[$i]
  $msg = $opcodeMsgs[$i]
  $task = $opcodeTasks[$i]

  $taskNode = $xml.SelectSingleNode("//*[local-name()='task' and @name='$task']")

  $node = $xml.CreateElement('opcode', 'http://schemas.microsoft.com/win/2004/08/events')
  $node.SetAttribute('value', $opcode)
  $node.SetAttribute('name', $name)
  $node.SetAttribute('mofValue', $opcode)
  $node.SetAttribute('message', $msg)
  $taskNode.AppendChild($node)
}

$xml.Save("$(resolve-path $filename).new")
