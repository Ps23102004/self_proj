# Mario Kart–Inspired 2.5D Racer (Unity 2022 LTS + NGO)

## Unity Version
- Unity 2022 LTS (2022.3.x)

## Required Packages
- **Netcode for GameObjects** (com.unity.netcode.gameobjects)
- **Unity Transport** (com.unity.transport)
- **Input System** (com.unity.inputsystem)

## Scenes
- `MainMenuScene`
- `LobbyScene`
- `RaceScene`

## How To Run
1. Open `MainMenuScene` and press **Play (Multiplayer)**.
2. In `LobbyScene`, click **Host** on one instance.
3. On another instance, click **Join** (use `127.0.0.1`).
4. Both players toggle **Ready**.
5. Host clicks **Start Race**.
6. Race loads, countdown starts, drive and complete 3 laps.
7. Results appear, host can return to lobby.

## Controls
- **Steer**: WASD / Arrow Keys / Gamepad Left Stick
- **Drift**: Space / Gamepad Right Shoulder
- **Brake**: Left Shift / Gamepad Left Trigger

## Networking Notes
- Server-authoritative movement via ServerRpc input.
- `NetworkTransform` should be set to **Server Authority** on the kart prefab.
- Client rigidbodies are kinematic; visuals update via `NetworkTransform`.

## Known Limitations
- No client-side prediction (expect small latency in movement).
- Basic arcade physics (intentionally simple).
- Player names are defaulted to “Player X”.

---

# Scene Setup (High Level)

## MainMenuScene
- Canvas with main panel (Play, Settings, Quit).
- Settings panel with a Back button.
- Add `MainMenuUI` component and wire buttons/panels.

## LobbyScene
- **NetworkManager** GameObject with:
  - `NetworkManager`
  - `UnityTransport`
  - `NetworkBootstrap`
- Lobby root GameObject with:
  - `NetworkObject`
  - `LobbyManager`
- Canvas with:
  - IP input field
  - Host / Join / Ready / Start Race / Quit buttons
  - Player list container
- Add `LobbyUI` to the canvas and wire UI references.

## RaceScene
- **RaceManager** GameObject with:
  - `NetworkObject`
  - `RaceManager`
- **RaceSceneBootstrap** GameObject with:
  - `RaceSceneBootstrap`
  - Assign `TrackSettings`, `KartSettings`, `RaceSettings`
  - Assign `RaceManager`
- **Camera** with `CameraFollow`.
- Canvas with HUD (lap, position, times, speed, countdown).
- Results panel with list + back button using `ResultsUI`.

## Kart Prefab
- Root GameObject with:
  - `Rigidbody` (gravity on)
  - `NetworkObject`
  - `NetworkTransform` (Server Authority)
  - `KartController`
- Create a simple kart from primitives (cube base + cylinders).
- Assign `KartSettings` in `KartController`.

---

# Script List
See `Assets/Scripts/...` for all scripts.

