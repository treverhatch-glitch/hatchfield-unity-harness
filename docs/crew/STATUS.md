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

## Not tested yet
- **The Unity editor side:** the game driver, the Play Mode controls test, the build script. They run once
  Unity is reachable from CI (licence secrets) or on Hatch's PC.
- **The online Unity build job** (needs the secrets).
- **Hatchfield's rules in Unity:** not started. Step one (repeatable web rules) is next.
- **Running the crew from Discord end to end:** the guide and rules are written; no full day has been run yet.

## Waiting on Hatch (steps in OWNER-STEPS.md)
1. Harness visibility: it is **public** today. Decide private (and how to keep its checks free), plus what to
   do with the two old public copies.
2. Unity licence secrets in GitHub.
3. The owner key (keygen), kept where the bots can't read it.
4. Branch protection: free on public repos; a private repo needs a paid GitHub plan.
5. OK to make `hatchfield-game` public, after the history check.
6. URP (plays in the browser too) or HDRP (best lighting, PC only).
