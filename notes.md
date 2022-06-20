
TODO: custom object pool
TODO: trash can

to test:
- item spawning
- item de-spawning

TODO: swimming script
TODO: script to change references to all kinds of things, most notably udon behaviours
TODO: door teleport setup editor tool

TODO: item sync: test unloaded players pickup up items
TODO: tool to mass replace object syncs with item sync



# Regex for adding, updating and removing debug messages in each method

```

"parameters" (replace manually): MethodNameToBeExcluded, ClassName

(( *)(private|public)\s+(override\s+)?\w+\s+(?!MethodNameToBeExcluded)(\w+)\([^\)]*\)(\s|\n)*\{)(\n\s*Debug\.Log\(\$"<vfx>.*)?

adding and updating:

$1
$2    Debug.Log($"<vfx> ClassName {this.name}  $5");

removing:

$1

```
