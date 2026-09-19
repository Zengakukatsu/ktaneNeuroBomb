# NeuroBomb

## Overview

This project combines the KTaNE Community ModKit and a modified version of the Neuro Unity SDK to allow integration of Neuro into *Keep Talking and Nobody Explodes*.

The Neuro SDK was modified to support the Unity 2017 version used by KTaNE. I adapted the SDK to work with the older C# and .NET 3.5 environment used by Unity 2017. This includes replacing `NativeWebSocket` with `websocket-sharp`.

All actual integration code is under `Assets/NeuroBomb`

The project is a work in progress. At this stage, menu navigation, bomb management and focus system have been implemented. Framework to accept module actions is in place however only a couple placeholder module actions have been made so far. Each module will have a handler with unique integration based on the module's needs. The end goal of the project is to integrate the full bomb-defusal process.

## Current Functionality

### Menu Navigation

Neuro can navigate the office and start a mission using:

- `open_binder` — Open the mission binder.
- `flip_page` — Navigate between mission pages.
- `select_mission` — Select an available mission.
- `start_mission` — Start the selected mission.
- `return_to_list` — Return to the mission list.

### Bomb Interaction

During a mission, Neuro can:

- `focus_module` — Focus a module on the bomb. Focused modules have their action windows exposed.
- `check_bomb_status` — Inspect the bomb’s remaining time, strikes, and solved-module progress.
- `check_sides` — Inspect bomb widgets (Battery count, Serial number, etc.)
- `cut_wire` — test action exposed when focused on a Simple Wires module. Allows wire cutting however ontext is incomplete.
- Use a generic placeholder handler given to unregistered modules in the `BombManager`

## Architecture

NeuroBomb is set up to have a single module focus. Only the focused module has its actions visible to Neuro, and she can swap focus or use other global defusal actions at any point in time. This is to prevent flooding context and make it easier for Neuro to understand each module.

`NeuroManager` is instantiated when the mod loads and tracks game scene changes. It creates the appropriate manager for the current game state.

`MenuManager` handles Neuro’s available actions while in the office, including opening the mission binder, navigating mission pages, selecting missions, and starting a game.

`BombManager` initializes after a gameplay round starts and scans components attached to the active bomb. It is responsible for:

- Tracking the currently focused module.
- Creating and managing focus action window.
- Creating and managing global action window.
- Creating and Closing module action windows as focus changes.
- Preventing overlapping actions while animations are running.

Module handlers are registered in `ModuleHandlerRegistry` and are responsible for:

- Module Context
- Module Actions
- Validation and execution logic for module interactions
- Keeping track of module state.
- They are created as needed during the Bomb scan and are stored in the `BombManager`

`ModuleHandlerRegistry` maps game `BombComponent` types to their dedicated handlers. Unsupported components fall back to a generic handler when possible.

Game interactions are performed through KTaNE’s internal `Selectable` system. The helper routines reproduce selection, focus, interaction, and deselection behavior as if a player hovered or clicked instead of directly changing module state.

## Current Limitations

- Not all modules have been integrated.
- End of mission navigation is not yet complete.
- Support for modded modules is not yet available.
- No action to pick up the bomb yet, it must be manually clicked.
- Context for action results (Strike, Solved, etc) not yet implemented.
- Focusing on a module on the non visible side of the bomb fails to flip the bomb around. (Solved previously, it is possible.)
- Leaderboards and Best time on the Mission Detail page load asynchronously. Either it must be sent as context once loaded after the Action window for the detail page has been made, or the Action window creation must be stalled until Leaderboards are added so they can be included in context.

## Usage

*Game assemblies are excluded from the repository to avoid distributing game files. They must be imported from a local KTaNE installation during step 3.*

1. Clone or download this repository.
2. Open the project in Unity `2017.4.22f1`.
3. Under `Keep Talking ModKit`, click `Import Assembly-CSharp` and select your local *Keep Talking and Nobody Explodes* Steam installation.
4. Reload the Unity project after the assembly import completes.
5. Under `Keep Talking ModKit`, open `Configure Mod` and fill out the required mod information.
6. Select `Build Asset Bundle`.
7. Copy the generated build files and `websocket-sharp.dll` into the KTaNE `Mods` folder.

Any prefab marked for `mod.bundle` will be included in the built asset bundle. Any prefab with a `KMService` component will be instantiated immediately after mods are loaded. Creating actions and windows works the same way as the base Neuro Unity SDK.

## Licenses

This project incorporates code and resources from multiple projects. The applicable license notices are included in the repository.

📁 `LICENSES/`  
├── [`KTANE_MODKIT_LICENSE.txt`](LICENSES/KTANE_MODKIT_LICENSE.txt)  
├── [`NEURO_SDK_LICENSE.md`](LICENSES/NEURO_SDK_LICENSE.md)  
├── [`NEWTONSOFT_JSON_LICENSE.md`](LICENSES/NEWTONSOFT_JSON_LICENSE.md)  
└── [`WEBSOCKET_SHARP_LICENSE.txt`](LICENSES/WEBSOCKET_SHARP_LICENSE.txt)
