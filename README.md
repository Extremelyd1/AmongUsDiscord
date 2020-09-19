# Among Us Discord
A tool to handle discord muting/deafening for Among Us

## What is Among Us Discord
Among Us Discord is a user-application that runs on your computer.
It's goal is to provide an immersive experience when playing with a group that is in a Discord call.
It accomplishes this by muting/deafening the user at appropriate moments.
Namely, all users are muted and deafened at the start of the game.
Users are only unmuted and undeafened if they are still alive when a meeting is called.
After a meeting, all alive players are muted and deafened again to allow dead players to speak among themselves.

## Usage
This tool only works if you run Discord and the game Among Us on the same device.
More specifically, this program is made for Windows, thus mobile Discord/Among Us users are not supported.
- Go to the [releases page](https://github.com/Extremelyd1/AmongUsDiscord/releases) and download the latest `Launcher.exe`.
- Go to Discord -> User Settings -> Keybinds, and create new keybinds for the following:
    - A keybind with Action `Toggle Mute` on keys `CTRL + F1`
    - A keybind with Action `Toggle Deafen` on keys `CTRL + F2`
- Run the `Launcher.exe` (with administrative rights if asked), and voila!
 
The program will automatically launch Among Us if it is not running yet. 
 
Note that the program might not run if you don't have `.NETFramework v4.7.2` installed. If the program does not start or gives an error, please check this first.

## Technical details
### Among Us directory location
The launcher automatically finds the install location of Among Us by finding the main Steam install path from registry.
This main Steam Library contains a file with entries pertaining to all registered Steam Library locations on disk. 
The launcher then checks each of these Library locations for the existence of the Among Us directory.

### Memory Reading
The way the program is able to distinguish game states and mute/deafen accordingly is by reading the process memory.
The structs outlined in `AmongUsDiscordIntegration/Structs` are exactly stored in this way in memory. 
By finding a stable pointer (or pointer path) to the start of these structs in the process memory of Among Us, we can mimic their structure with values in the program.
