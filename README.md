# 🚤 River Rush — Tropical Boat Adventure

An arcade 3D boat-running game featuring tropical river environments, responsive boat steering physics, dynamic obstacles, coin collection, and multiple unlockable boats.

This repository contains both:
1. **Interactive WebGL 3D Browser Game** (`preview/index.html`) powered by Three.js with realistic stylized water shaders, procedural banks, rocks, waterfalls, foliage, particle effects, and touch/keyboard controls.
2. **Unity 3D Project** (`Assets/`, `Packages/`, `ProjectSettings/`) configured for Unity 2022.3.62f3 LTS with complete scene architectures, C# gameplay controllers, and UI systems.

---

## 🎮 Play the Web Game (`preview/index.html`)

The web game runs directly in any modern browser via a lightweight local HTTP server.

### Option 1: Using Node.js (Built-in `npx`)
```bash
# Navigate to the preview directory
cd preview

# Start a local web server (any port, e.g., 8080)
npx serve -l 8080
```
Open **`http://localhost:8080`** in your browser.

### Option 2: Using Python 3
```bash
cd preview
python -m http.server 8080
```
Open **`http://localhost:8080`** in your browser.

### Option 3: VS Code / IDE Live Server
Right-click `preview/index.html` in VS Code and choose **"Open with Live Server"**.

> **Note:** Browsers enforce CORS policies on textures, fonts, and Web Audio. Opening `preview/index.html` via `http://` (local server) is recommended rather than `file://`.

---

## 🕹️ Controls

| Action | Keyboard / Mouse | Mobile / Touch |
| :--- | :--- | :--- |
| **Steer Left** | `A` or `Left Arrow` | Tap / Hold Left On-Screen Arrow |
| **Steer Right** | `D` or `Right Arrow` | Tap / Hold Right On-Screen Arrow |
| **Boost Throttle** | `W` or `Up Arrow` | Forward button / Auto-accelerate |
| **Brake / Reverse** | `S` or `Down Arrow` | Auto-drag |
| **Pause Game** | `Escape` or `P` | Tap Pause button in top HUD |
| **Menu Navigation** | Mouse Click | Touch Tap |

---

## 🧪 Automated Gameplay Verification

The repository includes a headless Chrome/Edge DevTools Protocol (CDP) automated test harness:

```bash
# Ensure local preview is running on port 8080
# Run the 9-stage comprehensive regression test suite:
node verify_gameplay_full.js
```

The test validates:
- [x] Initial Main Menu render & logo visibility
- [x] Boat selection carousel & boat switching
- [x] Map selection cards
- [x] Live 3D gameplay start & WebGL canvas rendering
- [x] Boat steering responsiveness & lateral delta
- [x] HUD distance ticker & coin counters
- [x] Pause menu modal toggling
- [x] Collision detection & lives decrementing
- [x] Game Over screen & score reporting

---

## 🛠️ Unity Project Setup

- **Unity Version:** `2022.3.62f3` (LTS)
- **Render Pipeline:** Built-in / URP Compatible
- **Main Scene:** `Assets/_Project/Scenes/RiverRush_MainScene.unity`

### Getting Started:
1. Open **Unity Hub**.
2. Click **Add** -> **Add project from disk**.
3. Select this repository's root folder (`River Rush`).
4. Ensure the editor version is set to `2022.3.62f3` (or compatible 2022.3 LTS).
5. Open the project. Unity will automatically restore the `Library/` cache from `Packages/manifest.json` and `Assets/`.
6. Open `Assets/_Project/Scenes/RiverRush_MainScene.unity` and press **Play**.

---

## 📁 Repository Structure

```
River Rush/
├── Assets/                        # Unity source assets
│   ├── _Project/
│   │   ├── Materials/             # Shaders & materials
│   │   ├── Prefabs/               # Game object prefabs
│   │   ├── Scenes/                # RiverRush_MainScene.unity
│   │   ├── Screenshots/           # High-resolution UI captures
│   │   ├── Scripts/               # C# Controllers (UI, Scene, Build)
│   │   └── Textures/              # Sprites, backgrounds, UI icons
│   └── TextMesh Pro/              # TMP fonts, resources, and shaders
├── Packages/                      # Unity package manager manifest & lockfile
├── ProjectSettings/               # Unity engine configuration files (22 settings)
├── preview/                       # Active WebGL 3D Game
│   ├── assets/
│   │   ├── env/                   # 2K/4K environment skyboxes & backdrops
│   │   └── ui/                    # UI icons, buttons, boat cards, map previews
│   ├── gameplay_verified.png      # Reference gameplay screenshot
│   └── index.html                 # Complete Three.js WebGL game implementation
├── ReferencePanels/               # Canonical UI panel reference slices
├── River Rush_ Tropical Boat Adventure UI.png  # Master UI concept artwork
├── verify_gameplay_full.js        # Automated CDP gameplay test suite
├── verify_gameplay.js             # Core gameplay verification runner
├── verify_ui_layout.js            # Multi-viewport responsive UI test runner
├── .gitignore                     # Optimized Unity + Web ignore configuration
└── README.md                      # Project documentation
```

---

## ⛵ Features & Visual Systems

- **Procedural 3D River & Terrain:** Endless curved river with custom water shaders, natural turquoise gradients, animated flow displacement, caustic highlights, and foam wakes.
- **Dynamic Scenery:** Lush stylized palm trees, mossy cliffs, cascaded waterfalls, boulders, and distant mountain vistas with atmospheric fog.
- **5 Unique Boats:** Speedster (Red), Hydro Wave (Blue), Thunderbolt (Yellow), Viper Jet (Green), and Phantom X (Black).
- **Audio Synthesis:** Web Audio API sound generator for engine rumble, water splash, coin ding, crash effects, and ambient river hum (zero external audio file dependencies).
- **Responsive Mobile UI:** Adapts from compact smartphones to ultra-wide desktop displays with floating steering controls and speedometer gauges.

---

## 📄 License
This project is for demonstration, development, and portfolio purposes. All proprietary game artwork and assets are reserved.
