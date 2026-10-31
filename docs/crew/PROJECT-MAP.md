# The two projects, and how they stay in step

```
 hatchfield-game (web, TypeScript + three.js)          hatchfield-unity-harness (Unity 6000.6, HDRP)
 ─────────────────────────────────────────────          ──────────────────────────────────────────────
 the game people can play today, in a browser           the port: the main version, a Windows game
 src/game/sim.ts …  the rules                           Assets/Sim/   the rules, pure C# (no Unity code)
 browser build: hatchfield-latest.pages.dev             Assets/Game/  input, drawing, UI (Unity)
           │                                                       ▲
           │  pinned comparison point ──────────────────────────── │
           │  "the Unity rules match the web rules at commit abc1234"
           ▼                                                       │
 web changes after the pin  ──►  npm run engine -- drift --queue  ──►  one port task each
```

## The rules of the road

- **Both keep moving.** Web work and Unity work land at the same time, in separate repos and separate tasks.
  Web work never waits for Unity.
- **The pin.** The Unity rules are compared with the web rules at one pinned commit, not with the live web
  game (`npm run engine -- port --from <sha> --source game/hatchfield/src/game`). Web work still in progress
  doesn't fail Unity work.
- **Parity.** `npm run parity` plays the same seeds in both and compares the results (outcome, tick, then
  cash, passengers, queues as each part is ported). A part counts as ported only when it matches.
- **Drift is work, not a failure.** `npm run engine -- drift` lists the web commits since the pin; `--queue`
  turns each into a port task. Each port task ends by moving the pin forward. The hourly status shows
  "Unity port N web change(s) behind".
- **When to stop growing the web version:** once the port is caught up (0 behind) and the Unity build is the
  one people play. Until then, new mechanics are tried in the web build first.

## Playing each one

| Version | How |
| --- | --- |
| Web | the browser build, or `npm run dev` in the web game folder |
| Unity, in the editor | open this folder in Unity Hub, press Play |
| Unity, on Windows | `npm run engine:build -- <unity> --target windows` → `Build/Windows/Game.exe` |

The Unity version uses HDRP at maximum quality, so it is a Windows game only; browser links stay on the web
version.

## First step of the port (in progress)

The web rules use `Math.random` in about 116 places, so the same game can't be replayed, and there's nothing
exact to compare the Unity rules with. Step one makes the web rules repeatable: one seeded random number
generator in the game state, used everywhere the rules roll dice. Then goldens (recorded runs) for the web
game. Then the Unity rules are ported a part at a time, each matched against those runs.
