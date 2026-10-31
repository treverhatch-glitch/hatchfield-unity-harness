# Logs: where they live and how to write in them

The logs hold Hatchfield detail, so they live in the **private harness repo** at `studio/logs/`, never in this
public repo.

| Log | File (in the harness) | Who writes | When |
| --- | --- | --- | --- |
| Work log | `studio/logs/WORK-LOG.md` | every bot and worker | after every task |
| Problems and workarounds | `studio/logs/PROBLEMS.md` | anyone who hits a problem | when it happens; again when it's fixed |
| Crew log (Discord) | `studio/logs/CREW-LOG.md` | the Ops bot (Haiku), from the channel | daily |
| Upgrade proposals | `studio/logs/PROPOSALS.md` | Claude (harness owner) | after each big step |

The format of each is at the top of its file. Never put a key, token, password or licence in a log. Link a
screenshot or recording; don't paste long output.

**For Hatch, the 2-minute read:** `studio/logs/README.md` has the latest summary at the top: what got done,
open problems, and the upgrade proposals waiting for your OK.
