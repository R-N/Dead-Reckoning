# Repository Guide

## Layout

- Unity `2023.1.12f1` project. Only configured build scene: `Assets/Scenes/SampleScene.unity`.
- `Assets/Scripts/GameManager.cs` writes per-player `states.csv` and owns recording-run lifecycle (start/finalize/discard-on-failure).
- `Assets/Scripts/CameraManager.cs` captures four semantic grayscale channels per fixed tick — `bullet`, `enemy`, `buff`, `obstacle` — via async GPU readback and a background JPEG writer thread.
- Fixed timestep is `0.02s` (50 Hz), independent of the 60 FPS render target; state rows and image frames are sampled together in `FixedUpdate`.
- Recording output lands under `Assets/.Data/` (`Application.dataPath/.Data`), one directory per run. Consumed by the `dead_reckoning_forecast` Python package (training/inference in `model/process.py`), joined by numeric `frame`.

## Working here

- Unity Test Framework `1.3.7` is installed; no test sources are checked in. Run changed gameplay/recording behavior from `SampleScene`.
- `Assets/.Data/` is not ignored by `.gitignore` — inspect/remove generated captures before committing.
- Do not commit Unity-generated `Library/`, `Temp/`, `Obj/`, builds, logs, or generated `.sln`/`.csproj` files.

<!-- CODEGRAPH_START -->
## CodeGraph

In repositories indexed by CodeGraph (a `.codegraph/` directory exists at the repo root), reach for it BEFORE grep/find or reading files when you need to understand or locate code:

- **MCP tool** (when available): `codegraph_explore` answers most code questions in one call — the relevant symbols' verbatim source plus the call paths between them, including dynamic-dispatch hops grep can't follow. Name a file or symbol in the query to read its current line-numbered source. If it's listed but deferred, load it by name via tool search.
- **Shell** (always works): `codegraph explore "<symbol names or question>"` prints the same output.

If there is no `.codegraph/` directory, skip CodeGraph entirely — indexing is the user's decision.
<!-- CODEGRAPH_END -->
