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
    - the web rules inside a Windows build (.exe); the build itself has no rules file in it yet
    - the replay check inside the Unity editor (the Edit Mode tests have not been run in the editor yet)
    Proven, with the proof in docs/crew/STATUS.md: the HDRP set-up on Hatch's PC; Play Mode with HDRP; the web
    rules running in Play Mode through Jint (day 1 matches .NET exactly); the CI Windows build with HDRP.
    Nobody writes "works", "done" or "fixed" for these without that proof linked next to it.
12. **The web game's packed rules file never goes in this repo** (it is the private game's code). It is built on
    Hatch's PC into `Rules~/` (ignored by Git and by Unity). The guard blocks it by name and by content.

13. **React, then report, on Discord.** The moment a crew bot gets a task message, it reacts 👀. When the task is
    done, it reacts ✅ on the same message, then posts a short result (what changed, the PR or log link, the proof,
    and anything UNTESTED) that @mentions the grokbot game development bot (`<@1565786850763743272>`).
14. **When the grokbot game development bot prompts you, answer it by name.** Whenever the grokbot game development
    bot (Discord id `1565786850763743272`) prompts a crew bot, that bot's reply @mentions it
    (`<@1565786850763743272>`), so it always knows what got done.
15. **Every fix goes through the harness**, whatever its size: a harness task (claim it), a review, a screenshot or
    log line as proof, and entries in the work log and the problems log. No fix lands outside that loop.
16. **Untested things are marked untested**, in the Discord result, the PR and the logs (rules 7 and 11).
