![Hollow Knight Multiplayer](https://i.imgur.com/ZejexKS.png)

## What is Hollow Knight Multiplayer?
As the name might suggest, Hollow Knight Multiplayer (HKMP) is a multiplayer mod for the popular 2D action-adventure game Hollow Knight.
The main purpose of this mod is to allow people to host games and let others join them in their adventures.
A few of the core components of this mod are heavily inspired from an existing implementation found [here](https://github.com/jngo102/HollowKnight.Multiplayer).
However, as it seemed to be discontinued, I decided to rework it entirely and add extra features where possible.

## Install
The mod works through the [Hollow Knight Modding API](https://github.com/seresharp/HollowKnight.Modding) (a getting started guide can be found [here](https://radiance.host/apidocs/Getting-Started.html)).
After installing the API, this mod can be installed by dropping the compiled DLL into your mods folder, which can be found in your Steam installation: `<steam>/Hollow Knight/hollow_knight_Data/Managed/Mods/`.
The latest version of the compiled DLL can be found on the [releases page](https://github.com/Extremelyd1/HKMP/releases).

## Usage
The main interface of the mod can be found in the pause menu in-game.
There is an option to host a game on the entered port and an option to join a game at the entered address and entered port.
Playing multiplayer with people on your LAN is straightforward, but playing over the internet requires some extra work.
Namely, the port of the hosted game should be forwarded in your router to point to the device you are hosting on.
Alternatively, you could use software to facilitate extending your LAN, such as [Hamachi](https://vpn.net).

The interface can also be hidden by pressing a key-bind (right ALT by default). This key-bind can be changed in the config for the mod, which can be found at the following locations depending on OS:
- **Windows**: `%appdata%\..\LocalLow\Team Cherry\Hollow Knight\HKMP.GlobalSettings.json`
- **Mac**: `~/Library/Application Support/unity.Team Cherry.Hollow Knight/HKMP.GlobalSettings.json`
- **Linux**: `~/.config/unity3d/Team Cherry/Hollow Knight/HKMP.GlobalSettings.json`

The key-binds are stored in integer form, to find which key corresponds to which integer, please consult [this gist](https://gist.github.com/Extremelyd1/4bcd495e21453ed9e1dffa27f6ba5f69).

### Standalone server
It is possible to run a standalone server on Windows, Linux and Mac.
The latest executable of the server can be found on the [releases page](https://github.com/Extremelyd1/HKMP/releases).
For Linux and Mac, the server can only be run with [Mono](https://www.mono-project.com) installed.
After installing Mono, the same executable can be run using `mono HKMPServer.exe [args]`.
The server will read/create a settings file called `gamesettings.json`, which can be changed to alter the default startup settings of the server.
Alternatively, settings can be changed by running commands on the command line.
The following are the available commands:
- `set <setting name> [value]`: Read or write a setting with the given name and given value.
  To get a list of available settings, please see the subsections below.
- `exit`: Will gracefully exit the server and disconnect its users.

### Settings
The interface of the mod also contains a client and server settings menu

#### Client settings
The client settings contain the following entries:
- **Team Selection**: Allows the player to change their current team.
  This setting can only be changed when it is enabled server-side.
- **Player skin ID**: Allows the player to input the ID of the skin they would like to change into.
  After pressing the "Apply skin" button, this will locally change the skin and transmit it to the server.
  This only has effect if skins are enabled on the server.
- **Display ping**: Whether a ping display is enabled in the top-left side of the in-game screen.
  This will display the current RTT (round trip time) of the client-server connection.

#### Server settings
Note that this menu is entirely server-sided, only the player hosting the server will be able to alter the settings.
Moreover, the settings will only update server and client-side once the "Save settings" button is pressed.  
An explanation of the settings can be found below (in brackets their internal names for use in standalone server):
- **Enable PvP** (`IsPvpEnabled`): whether player vs. player damage is enabled.
- **Enable body damage** (`IsBodyDamageEnabled`): whether contact damage is enabled, namely when player models touch, both of them will be damaged.
  This only has effect if PvP is also enabled.
- **Always show map locations** (`AlwaysShowMapIcons`): whether player's map locations are always shared on the in-game map.
- **Only broadcast map with Wayward Compass** (`OnlyBroadcastMapIconWithWaywardCompass`): whether a player's map location is only shared when they have the Wayward Compass charm equipped.
  Note that if map locations are always shared, this setting has no effect.
- **Display names** (`DisplayNames`): Whether overhead names should be displayed.
- **Enable teams** (`TeamsEnabled`): Whether player teams are enabled.
  Players on the same team cannot damage each other.
  Teams can be selected from the client settings menu.
- **Allow skins** (`AllowSkins`): Whether player skins are allowed.
  If disabled, players will not be able to use a skin locally, nor will it be transmitted to other players.

The rest of the settings contain entries for damage values of most PvP enabled spells and abilities.
Inputting a value of `0` will completely disable the damage.
Following is a list of the internal names for use in the standalone server:
`NailDamage`, `GrubberflyElegyDamage`, `VengefulSpiritDamage`, `ShadeSoulDamage`, `DesolateDiveDamage`, `DescendingDarkDamage`,
`HowlingWraithDamage`, `AbyssShriekDamage`, `GreatSlashDamage`, `DashSlashDamage`, `CycloneSlashDamage`, `SporeShroomDamage`,
`SporeDungShroomDamage`, `ThornOfAgonyDamage`.

### Skins
Skins can be installed by dropping a folder into the skins directory (`<steam>/Hollow Knight/hollow_knight_Data/Managed/Mods/HKMP/Skins`).
If this directory structure is not present yet, it should be generated once you have launched the game at least once with HKMP installed.
This folder can be named anything, but should at least contain a `Knight.png` and a `Sprint.png` file.
These files should be a texture sheet that Hollow Knight also normally uses.
After running the game with skins installed, each of these skin directories should have a corresponding `id.txt` file generated.
This ID file contains a single integer representing the ID of that skin.
This ID can then be used in-game to select the skin from the client settings menu.
Normally, these IDs start at `1` and incrementally increase the more skins you use, but it is possible to manually edit the ID files to use other IDs.

## Discord server
You can also join the [Discord server](https://discord.gg/KbgxvDyzHP) for the mod.
There you can also leave your suggestions and bug reports or generally talk about it.
Moreover, the latest announcements will be posted there.

## Build instructions
HKMP can also be built from scratch.
This requires a few dependencies from the Hollow Knight game and the modding API.
Namely, the following dependencies should be added as referenced assemblies from **the modding API**:
- Assembly-CSharp.dll (modified by the modding API)

And the following assemblies should be added as references from **the Hollow Knight game/Unity**:
- PlayMaker.dll
- UnityEngine.AudioModule.dll
- UnityEngine.CoreModule.dll
- UnityEngine.dll
- UnityEngine.ImageConversionModule.dll
- UnityEngine.ParticleSystemModule.dll
- UnityEngine.Physics2DModule.dll
- UnityEngine.TextRenderingModule.dll
- UnityEngine.UI.dll
- UnityEngine.UIModule.dll

After this the source code can be compiled into DLL, and you should be good to go!

## Github issues
If you have any suggestions or bug reports, please leave them at the [issues page](https://github.com/Extremelyd1/HKMP/issues).
Make sure to label the issues correctly and provide a proper explanation.
Suggestions or feature requests can be labeled with "Enhancement", bug reports with "Bug", etc.

## Donations
If you like this project and would like to donate, you can do so via [Paypal](https://www.paypal.com/donate?hosted_button_id=QMB2XYX3W9W6A).
Please only donate if you really want to, there's no obligation in doing so.
