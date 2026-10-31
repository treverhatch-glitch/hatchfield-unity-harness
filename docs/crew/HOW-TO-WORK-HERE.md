# How to work here

## Where things live

| What | Where | Visible to |
| --- | --- | --- |
| The studio harness (tasks, checks, lanes, logs) | the harness folder: a clone of its own private repo | Hatch and the crew |
| The web game (TypeScript, three.js) | adopted into the harness at `game/hatchfield/`; its own repo is `hatchfield-game` | private |
| The Unity port (Unity 6000.6, HDRP at max quality, a Windows game) | this repo, cloned beside the harness folder | public |

Run harness commands from the harness folder. Unity commands take this repo's path (below, `<unity>`).

## A task, start to finish

```bash
export STUDIO_LOCK_OWNER=sonnet-1                       # a unique name per bot (Windows: set STUDIO_LOCK_OWNER=sonnet-1)
npm run task -- next                                    # what's ready for your role
npm run task -- start T-123 --agent sonnet-1            # claim it; if it refuses, it's someone else's
# ... work only inside the task's lane or files ...
npm run task -- done T-123 --agent sonnet-1 --check     # seconds: says what's still missing
npm run verify                                          # the checks, in the foreground
npm run task -- done T-123 --agent sonnet-1             # closes it, only when everything is green
npm run task -- pause T-123 --agent sonnet-1 --note "where I stopped and why"   # stopping early
```

Unity work (the commands point at this repo):

```bash
npm run engine:sim -- <unity>                           # the game rules: purity check + tests, no Unity needed
dotnet run --project tools/SimCli -- replay replays      # (in this repo) the Unity rules against the web game's replay files
npm run parity                                          # Unity rules vs web rules, same seeds (once the port is set up)
node harness/unity.mjs gate <unity>                     # Unity's own Edit Mode + Play Mode tests (needs Unity)
npm run engine:build -- <unity> --target windows        # the playable Windows game (Build/Windows/Game.exe)
npm run engine -- drift                                 # web changes the Unity port hasn't taken yet
```

## The game's rules in Unity (option A, decided 2026-10-31)

Unity runs the web game's own rules file; Unity only draws (HDRP), takes input and shows UI. The rules file is
the private game's code, so it is **built on Hatch's PC and never committed here**:

1. In the web game folder (the private repo), on the branch that has `tools/replay/vite.sim.config.ts`:
   `npx vite build --config tools/replay/vite.sim.config.ts` → `dist-sim/hatchfield-sim.mjs`.
2. Copy that file to this repo's `Rules~/hatchfield-sim.mjs` (Git and Unity both ignore `Rules~/`).
3. Check it: `dotnet run --project tools/SimCli -- replay replays` → `3/3 replay(s) match through the port`
   (about a minute per replay).

The same check is an Edit Mode test (`Assets/Tests/EditMode/RulesReplayTests.cs`, category `Slow`): in the Unity
editor, Window > General > Test Runner > EditMode > Run All; `dotnet test tools/SimTests` runs it without the editor
(`--filter TestCategory!=Slow` leaves it out). It plays the 3 replays through the rules file, one to three minutes
per seed, and the editor waits while it runs. Without `Rules~/hatchfield-sim.mjs` (CI, a fresh clone) it is
Ignored with the reason, not failed. Its output names the rules file's fingerprint (the first 12 hex digits of its
sha256, as `sha256sum` prints them; the rules host logs it too when it loads), so a stale copy shows. Running it
inside the Unity editor is UNTESTED.

The guard blocks the rules file here by name and by content. Running the rules inside the Unity editor is
UNTESTED (proven in plain .NET only).

## Proving work

"Done" is never just "done". Every finished task gets one of these in the work log:

- **Something you can see** (UI, a building, a camera change): a screenshot. In the harness:
  `npm run proof -- add T-123 <screenshot> "what it shows, in one line"`. A different bot, or Hatch, checks
  it matches.
- **Something that moves** (an animation, a flow): a short recording (`npm run shot` for a still; a few
  seconds of screen capture for motion).
- **Rules and numbers:** the test result line (for example `[unity] PASS sim: 12 test(s) passed`).

## Backing up before a big change

1. Commit and push what you have, on your own branch (`claude/<topic>`). Never on `main`.
2. Tag the point you can come back to: `git tag backup/2026-11-01-before-airport-grid && git push origin --tags`.
3. In the harness: `npm run learn -- backup` (copies the studio's decisions and notes to the backup folder).
4. Write in the work log what you backed up and why.

## Branches and pull requests

- One branch per task: `claude/<topic>`. Pull requests go to `main` and need the checks green.
- The pre-push guard runs on every push (turn it on once per clone: `git config core.hooksPath .githooks`).
