# HKMP <img src="https://files.catbox.moe/x2wnhc.svg" width="52" align="right">

## What is Hollow Knight Multiplayer?
As the name might suggest, Hollow Knight Multiplayer (HKMP) is a multiplayer mod for the popular 2D action-adventure game Hollow Knight.
The main purpose of this mod is to allow people to host games and let others join them in their adventures.
There is a dedicated [Discord server](https://discord.gg/KbgxvDyzHP) for the mod where you can ask questions or generally talk about the mod.
Moreover, you can leave suggestions or bug reports. The latest announcements will be posted there.

## Install
### Quick start
A [community-made guide](https://geroyuni.notion.site/HKMP-Hollow-Knight-Multiplayer-21723018c74c41d3bc555ee9cfaeb743) exists to get started easily with the mod.
If you are not experienced with Github and/or Hollow Knight modding, this is the recommended way to start using the mod.
Alternatively, the sections below illustrate how to get the mod from the installer or install it manually.

### Modding installer
The latest version of the mod can be found on [Scarab](https://github.com/fifty-six/Scarab), the modding installer for Hollow Knight 1.5.
This installer will automatically download the modding API and install the mod via an interface.

### Manual install
The mod works through the [Hollow Knight Modding API](https://github.com/hk-modding/api) (a getting started guide can be found [here](https://radiance.host/apidocs/Getting-Started.html)).
After installing the API, this mod can be installed by dropping the compiled DLL into your mods folder, which can be found in your Steam installation:
(Beware that these are the default locations. Your install may be on a different drive, in that case change your path accordingly.)

- **Windows**: `C:\Program Files (x86)\Steam\steamapps\common\Hollow Knight\hollow_knight_Data\Managed\Mods\`
- **Mac**: `~/Library/Application Support/Steam/steamapps/common/Hollow Knight/hollow_knight.app/`, then click "open package contents" and `content -> resources -> data -> managed -> mods`
- **Linux**: `~/.local/share/Steam/steamapps/common/Hollow Knight/hollow_knight_Data/Managed/Mods/`

The latest version of the compiled DLL can be found on the [releases page](https://github.com/Extremelyd1/HKMP/releases).

## Usage
The main interface of the mod can be found in the pause menu in-game.
There is an option to host a game on the entered port and an option to join a game at the entered address and entered port.
Playing multiplayer with people on your LAN is straightforward, but playing over the internet requires some extra work.
Namely, the port of the hosted game should be forwarded in your router to point to the device you are hosting on.
Alternatively, you could use software to facilitate extending your LAN, such as [Hamachi](https://vpn.net).

The interface can also be hidden by pressing a key-bind (`right ALT` by default). This key-bind can be changed in the config for the mod, which can be found at the following locations depending on OS:

- **Windows**: `%appdata%\..\LocalLow\Team Cherry\Hollow Knight\HKMP.GlobalSettings.json`
- **Mac**: `~/Library/Application Support/unity.Team Cherry.Hollow Knight/HKMP.GlobalSettings.json`
- **Linux**: `~/.config/unity3d/Team Cherry/Hollow Knight/HKMP.GlobalSettings.json`

The key-binds are stored in integer form, to find which key corresponds to which integer, please consult [this list](https://gist.github.com/Extremelyd1/4bcd495e21453ed9e1dffa27f6ba5f69).

In addition to the pause menu UI, there is a chat window that allows users to enter commands.
The chat input can be opened with a key-bind (`T` by default), which feature the following commands:
- `connect <address> <port> <username>`: Connect to a server at the given address and port with the given username.
- `host <start|stop> [port]`: Start a server on the given port or stop an existing server.
- `list`: List the names of the currently connected players.
- `set <setting name> [value]`: Read or write a setting with the given name and given value. For a list of possible
  settings, see the section below.
- `announce <message>`: Broadcast a chat message to all connected players.
- `kick <auth key|username|ip address>`: Kick the player with the given authentication key, username or IP address.
- `ban <auth key|username>`: Ban the player with the given authentication key or username. If given a username, will only
  issue the ban if a user with the given username is currently connected to the server.
- `unban <auth key>`: Unban the player with the given authentication key.
- `banip <auth key|username|ip address>`: Ban the IP of the player with the given authentication key, username or IP address.
  If given an auth key or a username, will only issue the ban if a user with the given auth key or username is currently
  connected to the server.
- `unbanip <ip address>`: Unban the IP of the player with the given IP address.

### Authentication/authorization
Each user will locally generate an auth key for authentication and authorization.
This key can be used to whitelist and authorize specific users to allow them to join
the server or execute commands that require higher permission.

- `whitelist [args]`: Manage the whitelist with following options:
  - `whitelist <on|off>`: Enable/disable the whitelist.
  - `whitelist <add|remove> [name|auth key]`: Add/remove the given username or auth key to/from 
  the whitelist. If given a username that does not correspond with an online player, the username will be
  added to the 'pre-list'. Then if a new player with a username on this list will login, they are automatically
  whitelisted.
  - `whitelist <clear> [prelist]`: Clear the whitelist (or the pre-list if `prelist` was given as argument).
- `auth [name|auth key]`: Authorize the online player with the given username or auth key.
- `deauth [name|auth key]`: De-authorize the online player with the given username or auth key.

### Standalone server
It is possible to run a standalone server on Windows, Linux and Mac.
The latest executable of the server can be found on the [releases page](https://github.com/Extremelyd1/HKMP/releases).
For Linux and Mac, the server can be run with [Mono](https://www.mono-project.com) installed.
After installing Mono, the same executable can be run using `mono HKMPServer.exe <port>`.
Currently, the only command-line argument is the port that the server should be hosted on.

The server will read/create a settings file called `gamesettings.json`, which can be changed to alter the default startup settings of the server.
Alternatively, settings can be changed by running the settings command on the command line.
In addition to the commands described above, the standalone server also has the following commands:
- `exit`: Will gracefully exit the server and disconnect its users.

### Settings
There are a lot of configurable settings that can change how the mod functions.
The client settings are available in the pause menu UI of the mod, while the sever
settings can be changed with the settings command.

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
This section contains the settings for the server. These values can be read and modified by the `set` command described above.
- `IsPvpEnabled`: whether player vs. player damage is enabled.
- `IsBodyDamageEnabled`: whether contact damage is enabled, namely when player models touch, both of them will be damaged.
  This only has effect if PvP is also enabled.
- `AlwaysShowMapIcons`: whether player's map locations are always shared on the in-game map.
- `OnlyBroadcastMapIconWithWaywardCompass`: whether a player's map location is only shared when they have the Wayward Compass charm equipped.
  Note that if map locations are always shared, this setting has no effect.
- `DisplayNames`: Whether overhead names should be displayed.
- `TeamsEnabled`: Whether player teams are enabled.
  Players on the same team cannot damage each other.
  Teams can be selected from the client settings menu.
- `AllowSkins`: Whether player skins are allowed.
  If disabled, players will not be able to use a skin locally, nor will it be transmitted to other players.

The rest of the settings contain entries for damage values of most PvP enabled spells and abilities.
Setting them to a value of `0` will completely disable the damage.
Following is a list of the internal names for use in the standalone server:
`NailDamage`, `GrubberflyElegyDamage`, `VengefulSpiritDamage`, `ShadeSoulDamage`, `DesolateDiveDamage`, `DescendingDarkDamage`,
`HowlingWraithDamage`, `AbyssShriekDamage`, `GreatSlashDamage`, `DashSlashDamage`, `CycloneSlashDamage`, `SporeShroomDamage`,
`SporeDungShroomDamage`, `ThornOfAgonyDamage`.

### Skins
Skins can be installed by dropping a folder into the skins directory (`<steam>/Hollow Knight/hollow_knight_Data/Managed/Mods/HKMP/Skins`).
If this directory structure is not present yet, it should be generated once you have launched the game at least once with HKMP installed.
This folder can be named anything, but the files should be texture sheets that Hollow Knight also normally uses.
After running the game with skins installed, each of these skin directories should have a corresponding `id.txt` file generated.
This ID file contains a single integer representing the ID of that skin.
This ID can then be used in-game to select the skin from the client settings menu.
Normally, these IDs start at `1` and incrementally increase the more skins you use, but it is possible to manually edit the ID files to use other IDs.

## Contributing
There are a few ways you can contribute to this project, which are all outlined below.
Please also read and adhere to the [contributing guide](https://github.com/Extremelyd1/HKMP/blob/master/CONTRIBUTING.md).

### Github issues
If you have any suggestions or bug reports, please leave them at the [issues page](https://github.com/Extremelyd1/HKMP/issues).
Make sure to label the issues correctly and provide a proper explanation.
Suggestions or feature requests can be labeled with "Enhancement", bug reports with "Bug", etc.

### Entity system
The entity system (or colloquially known as "enemy sync") is a system that allows entities (enemies and bosses in the game) to be synchronised across clients.
This system requires an implementation per distinct (complex) entity in its current implementation.
Each implementation requires a significant amount of work to finish and Hollow Knight features ~150 entities, of which at least 47 are complex (namely bosses).
Therefore, contribution is welcome and encouraged in order to help finish this system.
The following paragraphs outline how the entity system works and what constitutes a new entity implementation.

The way the system currently works is by sending the following data over the network: `position`, `scale`, `animation` (possibly with additional info), and `state`.
The `position` and `scale` are handled automatically by the system, but the animation and state need to be implemented.
The way to do this is by extending either the `Entity` or the `HealthManagedEntity` abstract classes in the `Hkmp.Game.Client.Entity` namespace.
The `HealthManagedEntity` class extends the `Entity` with functionality to handle entities that make use of a `HealthManager` component internally to determine when to do.
In most cases, the `HealthManagedEntity` will be the class to extend, but in some cases (for example for False Knight, see `FalseKnight.cs`) the dying sequences will be handled differently and thus not use the HealthManager component.
Most if not all entities in Hollow Knight are controlled by finite state machines (`FSM`) that are implemented in C# in with library PlayMaker.
States in these FSMs have actions that are executed when the state is entered.
Exact details are omitted here, but can probably be found in the modding channels in the [official HK discord](https://discord.gg/hollowknight) or the newer [HK modding discord](https://discord.gg/VDsg3HmWuB).
To inspect the FSMs of entities, the tool [FSMViewAvalonia](https://github.com/nesrak1/FSMViewAvalonia) can be used.
  
Both of the extendable classes contain abstract methods that need to be implemented, namely `InternalInitializeAsSceneHost`, `InternalInitializeAsSceneClient`, `InternalSwitchToSceneHost`, `UpdateAnimation`, and `UpdateState`.
- `InternalInitializeAsSceneHost`: This method will be called when the entity is initialized given that the local player is a scene host.
  This means that the entity needs to send animation and state updates and let the underlying FSM run its course.  
- `InternalInitializeAsSceneClient`: This method will be called when the entity is initialized, given that the local player is a scene client.
  We need to make sure the entity cannot progress to another FSM state on its own, but rather wait for animation and state updates and act accordingly.
  This method is called with an optional state index, which tells the client to initialize the entity at the given state.  
- `InternalSwitchToSceneHost`: This method will be called when the entity needs to be switch since the local player has suddenly become the scene host.
  This means that the entity needs to switch from not running its own FSM to proceeding to run its FSM in a proper manner.  
- `UpdateAnimation`: This method will be called when there is an entity animation received from the network.
  The entity needs to execute the proper actions to mimic the local animation.  
- `UpdateState`: This method will be called when there is an entity state received from the network.
  The entity needs to make sure it is already in that particular state, or update it so that it corresponds to that state.  
 
The Entity class contains a lot of utility methods to make it easier to manage entities, such as removing (and storing) all transitions of FSMs, restoring all transitions of FSMs, and sending animation and state updates.
Moreover, the `Hkmp.Fsm` namespace contains the `ActionExtensions` class that hosts a lot of methods to execute FSM actions a single time.
These methods are useful for replicating FSM behaviour by simply getting the action from the FSM and executing it.
The class also has convenience methods to execute a range of actions from an FSM given the state name and action indices.
Note: this class contains methods for a bunch of FSM actions, but it definitely not exhaustive, so if you come across an action type that hasn't been implemented yet, feel free to do so.
 
Currently, there exist implementations for a few entities, have a look at those before starting your own implementation to make sure you are not doing more work than necessary.
Apart from implementing the abstract methods, you should make sure that the entity sends animation and state updates at the proper times in the FSM (in the existing implementations, this method is usually called `CreateAnimationEvents`).
Regarding testing, you should have two instances of the game running at the same time and extensively test entering/leaving the scene of the entity at all possible states that the FSM can have.
 
If you have any more questions or remarks, do not hesitate to post them in the [discord server](https://discord.gg/KbgxvDyzHP)'s #entity-system-development.
If you want to start working on the implementation of an entity, either make a draft pull request or an issue. That way we can keep track of who is currently working on which entity to avoid accidentally overlapping.
When you finish the implementation and have tested it thoroughly, you can make a pull request.

Current progress on the entity system can be seen on the [project page](https://github.com/Extremelyd1/HKMP/projects/1).
If you decide to start implementing an entity you can either make a draft pull request, or open an issue and you'll be added to the project.
This way we can keep a clear overview of who is working on what at the moment and prevent duplicate work.

## Build instructions
### Client mod
The HKMP mod can also be built from scratch.
This requires a few dependencies from the Hollow Knight game and the modding API.
Namely, the following assemblies are needed from **the modding API**:
- `Assembly-CSharp.dll (modified by the modding API)`
- `MMHOOK_Assembly-CSharp.dll`

And the following assemblies are needed from **the Hollow Knight game/Unity**:
- `PlayMaker.dll`
- `UnityEngine.AudioModule.dll`
- `UnityEngine.CoreModule.dll`
- `UnityEngine.dll`
- `UnityEngine.ImageConversionModule.dll`
- `UnityEngine.InputLegacyModule`
- `UnityEngine.ParticleSystemModule.dll`
- `UnityEngine.Physics2DModule.dll`
- `UnityEngine.TextRenderingModule.dll`
- `UnityEngine.UI.dll`
- `UnityEngine.UIModule.dll`

All the files above can be found in the following directory based on your operating system (and might vary depending on installation):
- **Windows**: `C:\Program Files (x86)\Steam\steamapps\common\Hollow Knight\hollow_knight_Data\Managed`.
- **Mac**: `~/Library/Application Support/Steam/steamapps/common/Hollow Knight/hollow_knight.app/`, then click "open package contents" and `content -> resources -> data -> managed`
- **Linux**: `~/.local/share/Steam/steamapps/common/Hollow Knight/hollow_knight_Data/Managed`

With these assemblies handy (either in their original Hollow Knight directory or moved somewhere else) 
you should copy and rename the `HKMP/LocalBuildProperties_example.props` file to `HKMP/LocalBuildProperties.props`
and fill the paths in it to your locally used paths.
After this the source code can be compiled into a DLL, and you should be good to go!

### Standalone server
The standalone server can also be built from scratch.
There are technically two dependencies for the server:
- The built HKMP mod DLL (`HKMP.dll`)
- The Newtonsoft JSON library DLL (`Newtonsoft.Json.dll`)

The HKMP mod DLL is linked from the Release directory of the mod project and does not have to be manually copied.
The Newtonsoft JSON library, however, can be found in your modded Hollow Knight installation as denoted above.
This DLL should be placed in the `HKMPServer/Lib/` directory and will be embedded together with the HKMP DLL
during the build process.

Make sure to first build the HKMP mod before building the server to ensure the latest version is embedded.

## Patreon
If you like this project and are interested in its development, consider becoming a supporter on
[Patreon](https://www.patreon.com/Extremelyd1). You will get access to development posts, sneak peeks
and early access to new features. Additionally, you'll receive a role in the Discord server with access
to exclusive channels.

test
This is an exploit
testX