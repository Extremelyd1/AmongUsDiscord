# Among Us Discord
A partial integration to handle discord muting/deafening for the game Among Us.

## What is Among Us Discord
Among Us Discord is a BepInEx plugin for Among Us.
Its goal is to provide an immersive experience when playing with a group that is in a Discord call.
It accomplishes this by sending game-related events over HTTP to a webserver.
This application can then be coupled with a Discord bot that handles muting/deafening members based on these events.

## Usage
Since this is a plugin for BepInEx, you will need to install the BepInEx framework first.
A guide for this can be found on the [BepInEx documentation](https://docs.bepinex.dev/master/articles/user_guide/installation/unity_il2cpp.html).

**Note:** The above link refers to the documentation of a unreleased version of BepInEx and might not be accurate in the future.
As of writing this readme, this is the only documentation on installing the IL2CPP version of BepInEx.  

After installing the BepInEx framework successfully, the [latest release](https://github.com/Extremelyd1/AmongUsDiscord/releases/latest) of this plugin can be put in the BepInEx plugin folder.
The BepInEx plugin folder should be located relative to your Among Us installation: `[among-us]/BepInEx/plugins`. 
Running the game with the plugin installed at least once, will generate a config file at `[among-us]/BepInEx/config/AmongUsDiscord.cfg`.
This config file contains configurable settings for the webserver IP and port to send events to.

## Technical details for v2 and up
### BepInEx framework
Due to the use of the BepInEx framework the heavy lifting of hooking methods for game events is handled for us.
In contrast to v1.10 and below this condenses the codebase to a simple file with patches for methods and a file that manages the HTTP client.

## Technical details for v1.10 and below
### Among Us directory location
The launcher automatically finds the install location of Among Us by finding the main Steam install path from registry.
This main Steam Library contains a file with entries pertaining to all registered Steam Library locations on disk. 
The launcher then checks each of these Library locations for the existence of the Among Us directory.

### Memory Reading
The way the application is able to distinguish game states and mute/deafen accordingly is by reading the process memory.
The structs outlined in `AmongUsDiscordIntegration/Structs` are exactly stored in this way in memory. 
By finding a stable pointer (or pointer path) to the start of these structs in the process memory of Among Us, we can mimic their structure with values in the application.

### Comments on Discord Bots
Note that I have played around with using this application in conjunction with a Discord Bot to automate muting/deafening users.
The clear advantage of that is that only a single user is required to run this application client-sided in order to relay the game state information to the Bot.
However, after testing it seemed to when playing with 6+ players, the Bot will inevitably hit the rate limit of Discord's API.
This will stagnate all requests made and delay them to the point that players are not (un)muted/deafened in a timely manner.
If anyone has a way to optimize these requests or somehow avoid hitting the rate limit of Discord's API, I accept all suggestions.
