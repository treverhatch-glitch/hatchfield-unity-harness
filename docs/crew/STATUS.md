# Status (2-minute read) — 2026-10-31

## Tested and working
- **Harness 2.46 merged**, all online checks green. Includes:
  - the Unity track
  - playable Unity builds
  - running web and Unity side by side (pin, drift, parity against the pin)
  - adopting an existing web game; pressing a web game's real controls in a browser (`npm run play:controls`)
- **On a private copy of Hatchfield** (web):
  - adoption works; the related-tests check takes 36 s instead of 6 min
  - the whole-game check is green
  - Space, 2, 1 and H all pressed for real and checked
- **The Unity template's rules on .NET**, with no Unity needed:
  - 7 tests pass
  - they match the web template game on 6 seeds
  - planted Unity-only code and a planted rule break are both caught
- **This repo's public guard** blocks keys, licence files, harness files and private notes (tried with planted
  ones: 6 of 6 blocked).

## Proven on 2026-10-31
- **Unity licence in CI:** GameCI activated the Personal licence ("Successfully activated ULF license").
- **CI Windows build (URP sample, before the HDRP switch):** "Build Finished, Result: Success", a 41 MB
  `hatchfield-windows` download, about 8 minutes (PR #4's unity run).
- **The HDRP switch on Hatch's PC (Unity 6000.6.7f1, DX12):** the menu ran to "[hdrp] FINISHED", and every row
  in the HDRP Wizard (17.6.0) is green, Global and Current Quality.
- **Play Mode with HDRP:** the sample scene plays with the HDRP sky, and the Console shows 0 errors and 0
  warnings. Only the empty sample scene: no game, rules or art in it yet.

- **CI Windows build with HDRP:** "Build Finished, Result: Success", a 64 MB `hatchfield-windows` download, 23 min
  (PR #6's unity run).

## Not tested yet
- **The Unity editor side:** the game driver, the Play Mode controls test, the build script. They run once
  Unity is reachable from CI (licence secrets) or on Hatch's PC.
- **Max settings vs the PC's graphics card:** set for an RTX; check once the exact model is known.
- **Hatchfield's rules in Unity:** not started. Step one (repeatable web rules) is next.
- **Running the crew from Discord end to end:** the guide and rules are written; no full day has been run yet.

## Option A: Unity runs the web game's own rules (decided 2026-10-31)
- **Tested in plain .NET:** the packed web rules in Jint play all 3 replays and match every day's numbers
  (cash, passengers, flights, reputation, dice position): `3/3 replay(s) match through the port`.
- **Live play in .NET:** the same API Unity uses (new game, tick, what to draw): 60 ticks of city-start seed 1 reach
  the replay's day 1 exactly (cash 270,679, 118 passengers, 32x32 grid with 118 built tiles), ~0.4 s per tick.
- **Proven in the Unity editor (2026-10-31, Hatch's PC, Play Mode, HDRP):** `HatchfieldRulesDriver` loaded
  `Rules~/hatchfield-sim.mjs` ("[rules] loaded ... city-start seed 1"), drew the airport's tiles and flight NO101,
  and at tick 60 showed **day 1, cash 270,679, 118 passengers**, the same as .NET (rules from the Oct 14 trunk; main's rules give other numbers). Console: 0 errors, 0 warnings.
  One tick every 3 s at speed 1x, as designed.
- **UNTESTED:** faster speeds, longer runs (day 3), and the rules inside a Windows build.

## Tested on the Unity side (with .NET, no editor)
- The rules' dice match the web game's bit for bit (seed 1337, first three rolls and the state).
- The 3 web replay files (re-recorded on hatchfield-game main, 2026-10-31) load; day-3 numbers: seed 1 cash
  290,185 / 298 pax / 6 flights, seed 2 300,535 / 260 / 5, seed 3 301,438 / 269 / 6. The rules packed from main
  play all 3 in .NET/Jint: 3/3 match.
- The replay check says "not ported yet" (0/3) instead of passing, and a planted difference is named
  ("day 2: passengers …").

## Waiting on Hatch (steps in OWNER-STEPS.md)
1. Make the harness private, set up the runner on your PC, and make the two old copies private (decided).
2. Unity licence secrets in GitHub.
3. The owner key (keygen), kept where the bots can't read it.
4. Branch protection: free on public repos; a private repo needs a paid GitHub plan.
5. `hatchfield-game` stays private for now; the history check report comes first.
