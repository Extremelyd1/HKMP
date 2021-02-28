![Hollow Knight Multiplayer](https://i.imgur.com/ZejexKS.png)

## What is Hollow Knight Multiplayer?
As the name might suggest, Hollow Knight Multiplayer (HKMP) is a multiplayer mod for the popular 2D action-adventure game Hollow Knight. 
The main purpose of this mod is to allow people to host games and let others join them in their adventures.

## Install
The mod works through the [Hollow Knight Modding API](https://github.com/seresharp/HollowKnight.Modding) (a getting started guide can be found [here](https://radiance.host/apidocs/Getting-Started.html)). 
After installing the API, this mod can be installed by dropping the compiled DLL into your mods folder, which can be found at `~\Hollow Knight\hollow_knight_Data\Managed\Mods\`.

## Usage
The main interface of the mod can be found in the pause menu in-game. 
There is an option to host a game on the entered port and an option to join a game at the entered address and entered port. 
Playing multiplayer with people on your LAN is straightforward, but playing over the internet requires some extra work. 
Namely, the port of the hosted game should be forwarded in your router to point to the device you are hosting on. 
Alternatively, you could use software to facilitate extending your LAN, such as [Hamachi](https://vpn.net).

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