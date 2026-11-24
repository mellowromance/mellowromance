# mellowromance — Chapter 1 prototype

A story-driven 3D walking game (three.js, single HTML file): wake up on the subway, get off at an uncanny
station, and wander a sunken pastel city to find lost memories.

## Play
Open `game/index.html` in a browser (double-click). Needs an internet connection the first time
(three.js and the Galmuri pixel font load from jsDelivr).

Controls: arrow keys move · Shift run · Space jump · E wave · WASD / mouse drag camera · wheel zoom ·
F talk / look · Enter next line · H hide controls.

## Project layout
| Folder | What |
|---|---|
| `game/` | the game (`index.html`, everything embedded) |
| `01_REF/`, `03_Mood/`, `06_UI/` | references, mood boards, UI guide / logo / pattern |
| `02_Model/` | head / face maps, accessories (halo lace, wings) |
| `04_Clothes/` | outfit generator (`Outfit/build_outfit.ps1`) and texture maps |
| `05_Pet/`, `05_Enviorment/` | pet / environment references |
| `100_Render/` | test renders |

## Rebuilding the outfit (Windows)
```powershell
powershell -ExecutionPolicy Bypass -File 04_Clothes\Outfit\build_outfit.ps1
```
