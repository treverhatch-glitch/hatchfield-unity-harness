# How to work here

## Where things live

| What | Where | Visible to |
| --- | --- | --- |
| The studio harness (tasks, checks, lanes, logs) | `C:\Users\HatchTag\3d-deep-sim-studio-harness` (its own private repo) | Hatch and the crew |
| The web game (TypeScript, three.js) | adopted into the harness at `game\hatchfield\`; its own repo is `hatchfield-game` | public once Hatch OKs it |
| The Unity port (Unity 6000.6, URP) | `C:\Users\HatchTag\hatchfield-unity-harness` (this repo) | public |

Run harness commands from the harness folder. Unity commands take this repo's path.

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
npm run engine:sim -- ../hatchfield-unity-harness       # the game rules: purity check + tests, no Unity needed
npm run parity                                          # Unity rules vs web rules, same seeds (once the port is set up)
node harness/unity.mjs gate ../hatchfield-unity-harness # Unity's own Edit Mode + Play Mode tests (needs Unity)
npm run engine:build -- ../hatchfield-unity-harness     # a playable browser build (--target windows for an .exe)
npm run engine -- drift                                 # web changes the Unity port hasn't taken yet
```

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
