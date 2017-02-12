# CommandLineTool
CommandLine Tools Lite for Unity

To add it to your game, once you added the folder to your project.

1) Just drop the Console prefab in your first scene, it will set itself to not destroy on load so it'll stay for the 
lifetime of your game unless you destroy it manually.

2) To add a command create a static function and use the attribute 
[CommandLine(string commandLine, string helpText = "", object[] arguments = null)]

3) In game use ` key (next to 1) to open the console. Then start typing your command and it should suggests it. You can scroll through
suggestions by using down arrow (then up to go back to typing), and scroll through history of successful commands using up arrow 
(then down to go back to typing), just press Enter/Return to send your command. If there is an error, it will tell you.

-- Some commands are already pre-existing:

attach_unity_output X: where X is 1 or 0 to enable or disable the redirection of Unity's console to that one. It's disabled by default to avoid bloating
console_size X: where X is the height in pixels you want the console to take on screen
console_opacity X: where X is a float between 0 and 1
clear: to clear the console output

