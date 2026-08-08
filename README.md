# Dead Reckoning

Top-down shooter that records gameplay for
[dead_reckoning_forecast](https://github.com/R-N/dead_reckoning_forecast),
a position-forecasting model trained on the recorded runs.

Open in Unity `2023.1.12f1`. Only configured build scene:
`Assets/Scenes/SampleScene.unity`.

## Recording pipeline

- `Assets/Scripts/GameManager.cs` writes per-player `states.csv` (movement,
  cooldowns, dash, bullets) and owns the recording-run lifecycle — a run is
  finalized only once all camera buffers drain successfully, and is deleted
  on failure or an early application quit.
- `Assets/Scripts/CameraManager.cs` captures four semantic grayscale channels
  per fixed tick — `bullet`, `enemy`, `buff`, `obstacle` — via async GPU
  readback and a background JPEG writer thread, so recording never blocks
  the render loop.
- Fixed timestep is `0.02s` (50 Hz), independent of the 60 FPS render
  target; state rows and image frames are sampled together in
  `FixedUpdate` and joined downstream by numeric `frame`.
- Output lands under `Assets/.Data/` (`Application.dataPath/.Data`), one
  directory per run. Not covered by `.gitignore` — inspect/remove generated
  captures before committing.
