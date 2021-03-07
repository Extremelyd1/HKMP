![Hollow Knight Multiplayer](https://i.imgur.com/ZejexKS.png)

## What is Hollow Knight Multiplayer?
As the name might suggest, Hollow Knight Multiplayer (HKMP) is a multiplayer mod for the popular 2D action-adventure game Hollow Knight. 
The main purpose of this mod is to allow people to host games and let others join them in their adventures.
A few of the core components of this mod are heavily inspired from an existing implementation found [here](https://github.com/jngo102/HollowKnight.Multiplayer). 
However, as it seemed to be discontinued, I decided to rework it entirely and add extra features where possible. 

## Install
The mod works through the [Hollow Knight Modding API](https://github.com/seresharp/HollowKnight.Modding) (a getting started guide can be found [here](https://radiance.host/apidocs/Getting-Started.html)). 
After installing the API, this mod can be installed by dropping the compiled DLL into your mods folder, which can be found at `~\Hollow Knight\hollow_knight_Data\Managed\Mods\`.
The latest version of the compiled DLL can be found on the [releases page](https://github.com/Extremelyd1/HKMP/releases).

## Usage
The main interface of the mod can be found in the pause menu in-game. 
There is an option to host a game on the entered port and an option to join a game at the entered address and entered port. 
Playing multiplayer with people on your LAN is straightforward, but playing over the internet requires some extra work. 
Namely, the port of the hosted game should be forwarded in your router to point to the device you are hosting on. 
Alternatively, you could use software to facilitate extending your LAN, such as [Hamachi](https://vpn.net).

### Settings
The interface of the mod also contains a settings menu. 
Note that this menu is entirely server-sided, only the player hosting the server will be able to alter the settings.
Moreover, the settings will only update server and client-side once the "Save settings" button is pressed.  
An explanation of the settings can be found below:
- **Enable PvP**: whether player vs. player damage is enabled.
- **Enable body damage**: whether contact damage is enabled, namely when player models touch, both of them will be damaged.
This only has effect if PvP is also enabled.
- **Always show map locations**: whether player's map locations are always shared on the in-game map.
- **Only broadcast map with Wayward Compass**: whether a player's map location is only shared when they have the Wayward Compass charm equipped. 
  Note that if map locations are always shared, this setting has no effect.

## Build instructions
HKMP can also be built from scratch. 
This requires a few dependencies from the Hollow Knight game and the modding API.
Namely, the following dependencies should be added as referenced assemblies from **the modding API**:  
- Assembly-CSharp.dll (modified by the modding API)
- ModCommon.dll

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

## Suggestions
If you have any suggestions or bug reports, please leave them at the [issues page](https://github.com/Extremelyd1/HKMP/issues).
Make sure to label the issues correctly and provide a proper explanation.
Suggestions or feature requests can be labeled with "Enhancement", bug reports with "Bug", etc.

## Discord server
You can also join the [Discord server](https://discord.gg/KbgxvDyzHP) for the mod.
There you can also leave your suggestions and bug reports or generally talk about it.
Moreover, the latest announcements will be posted there.
