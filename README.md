# NeuroBomb

## Overview

This project combines the KTaNE Community ModKit and a modified version of the Neuro Unity SDK to allow Neuro to defuse bombs in *Keep Talking and Nobody Explodes*. 

NeuroBomb is intended to expose KTaNE’s existing information and controls to Neuro without solving the game for her. Actions reproduce normal player interactions, while module context describes what Neuro could reasonably observe. Neuro must still interpret the bomb, communicate with the manual user, remember relevant information, and decide which actions to take. This is done to preserve the "testing" nature of the game.

The Neuro SDK was modified to support the Unity 2017 version used by KTaNE. I adapted the SDK to work with the older C# and .NET 3.5 environment used by Unity 2017. This includes replacing `NativeWebSocket` with `websocket-sharp`.

All actual integration code is under `Assets/NeuroBomb`.

**Special thanks to sykym for keeping this project going.**

## Current Functionality

- Menu and mission-binder navigation
- Bomb inspection, rotation, and module focusing
- Context and interactions for all 11 vanilla non-needy modules
- Chair spinning
- Configurable action descriptions at runtime
- Supports completing missions 1.3, 2.1–2.4, and 3.1–3.7

## Supported Modules

- Simple Wires
- The Button
- Keypads
- Simon Says
- Who’s on First
- Memory
- Morse Code
- Complicated Wires
- Wire Sequences
- Mazes
- Passwords

## Configuration

`modSettings.json` controls features including:

- Neuro SDK WebSocket URL (IMPORTANT! THIS IS WHERE TO CHANGE IT)
- Action descriptions
- Whether leaderboard information is included in mission context

KTaNE creates a user-editable copy of the configuration when NeuroBomb is first loaded. On Windows, it is placed in:

`%USERPROFILE%\AppData\LocalLow\Steel Crate Games\Keep Talking and Nobody Explodes\Modsettings`

Edit the generated NeuroBomb settings file in that directory. Press `R` in-game to reload the configuration without restarting the game.

> The configuration contains placeholders for future features. Only the settings documented above are currently supported.

## Architecture

NeuroBomb exposes actions for only one focused module at a time. This keeps Neuro’s available actions and context manageable while global bomb actions remain accessible.

`NeuroManager` tracks scene changes and creates the appropriate manager:

- `MenuManager` handles office and mission-binder navigation.
- `BombManager` handles bomb inspection, module focus, and interactions.
- `PostGameManager` handles mission results, retries, and returning to the menu.

`ModuleHandlerRegistry` assigns each supported module its dedicated handler. Module handlers provide the module’s context, actions, and interaction logic. Unsupported modules use a generic handler when possible.

Interactions use KTaNE’s `Selectable` system to reproduce normal player actions such as highlighting, pressing, holding, and releasing controls.

## Usage

*Game assemblies are excluded from the repository to avoid distributing game files. They must be imported from a local KTaNE installation during step 3.*

1. Clone or download this repository.
2. Open the project in Unity `2017.4.22f1`.
3. Under `Keep Talking ModKit`, click `Import Assembly-CSharp` and select your local *Keep Talking and Nobody Explodes* Steam installation.
4. Reload the Unity project after the assembly import completes.
5. Under `Keep Talking ModKit`, open `Configure Mod` and fill out the required mod information.
6. Select `Build Asset Bundle`.
7. Copy the generated build files and `websocket-sharp.dll` into the KTaNE `Mods` folder.

If you have any questions about installation you can message me on discord.
If you are fine with just getting a ZIP of it instead you can just message me on discord or something.

Any prefab marked for `mod.bundle` will be included in the built asset bundle. Any prefab with a `KMService` component will be instantiated immediately after mods are loaded. Creating actions and windows works the same way as the base Neuro Unity SDK.

## Current Limitations

- No Needy Modules
- Requires user input to enable mods prior to Neuro taking over. (I will eventually patch this out)
- The module-knowledge system is not yet implemented.
- Although the game lifecycle has been verified additional testing and polish are in progress.
- Alarm action untested, it never went off for me. I will Force it to happen for testing later.

## Future Plans

- Add support for needy modules.
- Flesh out config to toggle various features or behaviors.
- Create a knowledge system, where when a twin solves a module they will be provided better context for solving it faster in the future.
- Modded modules are possible to add and integrate in this project, but I do not currently plan to do so.

I like feedback. If you wanna give me feedback just message me on discord or find the thread for this and post there.

## Licenses

This project incorporates code and resources from multiple projects. The applicable license notices are included in the repository.

📁 `LICENSES/`  
├── [`KTANE_MODKIT_LICENSE.txt`](LICENSES/KTANE_MODKIT_LICENSE.txt)  
├── [`NEURO_SDK_LICENSE.md`](LICENSES/NEURO_SDK_LICENSE.md)  
├── [`NEWTONSOFT_JSON_LICENSE.md`](LICENSES/NEWTONSOFT_JSON_LICENSE.md)  
└── [`WEBSOCKET_SHARP_LICENSE.txt`](LICENSES/WEBSOCKET_SHARP_LICENSE.txt)
