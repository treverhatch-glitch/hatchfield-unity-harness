# Things only Hatch can do

One step at a time. Never paste a key, token, password or licence file into a chat.

## 1. Make the harness private (and keep its checks free)

1. Open github.com/treverhatch-glitch/3d-deep-sim-studio-harness → **Settings** → scroll to **Danger Zone** →
   **Change repository visibility** → **Make private** → confirm.
2. Its checks use about 52 minutes per push, and private repos get 2,000 free minutes a month, so they run
   on your PC instead (a self-hosted runner, free, only ever for the private harness): Settings → **Actions**
   → **Runners** → **New self-hosted runner** → **Windows** → run the commands GitHub shows, in PowerShell,
   in a new empty folder. The page shows a one-time registration token: it goes into that PowerShell window,
   never into a chat. Then Claude points the harness checks at that runner.
3. Make the two old public copies of the harness private: open `hatchfield-ai-studio-template`, then
   `ANTI-GRAV` → **Settings** → **Danger Zone** → **Change repository visibility** → **Make private**. Don't
   delete them.

## 2. Unity licence secrets (for the online Unity tests and builds)

Do this in **hatchfield-unity-harness**, and in the harness repo if you want its Unity job to run.

1. On GitHub, open the repo → **Settings** → **Secrets and variables** → **Actions** → **New repository secret**.
2. Name: `UNITY_LICENSE`. Value: on your PC, open `C:\ProgramData\Unity\Unity_lic.ulf` in Notepad, press
   Ctrl+A, Ctrl+C, and paste it into the Value box. Click **Add secret**.
3. New secret `UNITY_EMAIL`: the email you sign in to Unity with.
4. New secret `UNITY_PASSWORD`: your Unity password.
5. Tell Claude "secrets added" (no values). If your Unity account uses two-step sign-in, say that too: it may
   need another route. *(Untested until the first run.)*

Secrets in a public repo are safe: GitHub never shows them, and other people's pull requests can't read them.

## 3. The owner key (only you can approve changes to protected files)

1. In the harness folder, on your own PC: `npm run approve -- keygen`. Set `STUDIO_OWNER_KEY` first to keep the
   private key somewhere the bots can't read (ask Claude which spot suits your PC).
2. Commit and push only the public half: `git add studio/keys/owner-ed25519.pub.pem`, then commit and push.
3. To approve a change to a protected file later: `npm run approve -- protected --note "why"`.

## 4. Branch protection (your answer: yes)

1. Repo → **Settings** → **Rules** → **Rulesets** → **New branch ruleset**.
2. Target: the default branch (`main`). Turn on **Require a pull request before merging**, **Require status
   checks to pass** (pick the CI checks), **Block force pushes** and **Restrict deletions**. Save.
3. Free for public repos. **A private repo needs a paid GitHub plan for this**, so for the private harness it
   costs money: your call.

## 5. Making `hatchfield-game` public (later)

It stays private for now. Before any change, Claude scans its whole history and lists every file, commit and
note that would become visible, and any key or private link. Nothing changes without your OK.
