# NeuroBomb

## Overview

This project combines the KTaNE Community ModKit and a modified version of the Neuro Unity SDK to allow integration of Neuro into *Keep Talking and Nobody Explodes*.

The Neuro SDK was modified to support the Unity 2017 version used by KTaNE. I adapted the SDK to work with the older C# and .NET 3.5 environment used by Unity 2017. This includes replacing `NativeWebSocket` with `websocket-sharp`.

The project is a work in progress. At this stage, menu navigation actions have been implemented to demonstrate the integration and action system. The end goal of the project is to integrate the full bomb-defusal process.

## Current Functionality

Neuro can currently:

- `open_binder` — Open the mission binder
- `flip_page` — Navigate between mission pages
- `select_mission` — Select available missions
- `start_mission` — Start a selected mission
- `return_to_list` — Return to the mission list

The project has a NeuroManager that is created when the mod is loaded that tracks scene changes. At the moment upon entering the main menu, it creates MenuManager which handles all action windows available to Neuro and the state while in the office.

## Usage

1. Clone or download this repository.
2. Open the project in Unity `2017.4.22f1`.
3. Under `Keep Talking ModKit`, select `Import Assembly-CSharp` and browse to the `Assembly-CSharp.dll` from your local *Keep Talking and Nobody Explodes* Steam installation.
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
