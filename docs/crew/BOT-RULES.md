# Rules for the bots

1. **One bot owns each task.** Claim it before touching anything. If a task is claimed, leave it alone. Two
   bots never edit the same files.
2. **The harness docs are the source of truth.** If this repo and the harness disagree, the harness wins.
   Report the conflict in the problems log; don't "fix" it by guessing.
3. **Nothing is deleted or rewritten unless Hatch asked.** No deleting files, branches, backups or repos. No
   force-push. No rewriting history.
4. **Stop and ask Hatch before** anything that changes gameplay, controls or story, spends money, uses a
   paid service, or makes anything public.
5. **No handing work to another bot** unless Hatch says so. A bot that can't finish pauses its task with a
   note (see HOW-TO-WORK-HERE.md), and Hatch decides who picks it up.
6. **"Done" needs proof:** a screenshot, a short recording or a test result, linked in the work log.
7. **Untested means untested.** Say so in the task note and the work log. Never round it up to "works".
8. **Mechanics only.** Copy ideas from other games, never their code, art, sounds or names.
9. **Never print, paste or commit keys, tokens or licence files.** They live in GitHub Secrets and `.env`
   files that are never committed.
10. **Log as you go:** the work log after every task, the problems log for every problem and workaround
    (LOGS.md).
11. **UNTESTED until someone has run it**, and it stays marked UNTESTED in every note, PR and log until there is a
    screenshot or a log line that shows it working:
    - the web rules running inside Unity (Jint in Unity; so far proven in plain .NET only)
    - the HDRP set-up (menu Hatchfield > Switch to HDRP)
    - Play Mode in the Unity editor
    - the Windows build (locally or in CI)
    Nobody writes "works", "done" or "fixed" for these without that proof linked next to it.
12. **The web game's packed rules file never goes in this repo** (it is the private game's code). It is built on
    Hatch's PC into `Rules~/` (ignored by Git and by Unity). The guard blocks it by name and by content.

