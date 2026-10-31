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

## 2. Unity licence secrets (for the online Unity build)

**First, make sure the licence file exists on your PC:**
1. Open **Unity Hub** and sign in. A Google sign-in is fine: you added a password, and the online build needs
   that email and password.
2. Hub → the gear (**Preferences**) → **Licenses**. You should see an active **Personal** licence. If not:
   **Add** → **Get a free personal license**.
3. In File Explorer, open `C:\ProgramData\Unity\`. You should see `Unity_lic.ulf`. (`ProgramData` is hidden: type
   the path into the address bar.)

**Then add three secrets** to **hatchfield-unity-harness** on GitHub:
4. Repo → **Settings** → **Secrets and variables** → **Actions** → **New repository secret**.
5. Name `UNITY_LICENSE`. Value: open `Unity_lic.ulf` in Notepad, press Ctrl+A, Ctrl+C, and paste it into the Value
   box. **Add secret**.
6. Name `UNITY_EMAIL`: the email of your Unity account. **Add secret**.
7. Name `UNITY_PASSWORD`: the Unity password you added. **Add secret**.
8. Tell Claude "secrets added" (no values). The next push runs the Windows build: the first one with HDRP takes
   30–60 minutes.

**About your account:** the online build signs in with email and password, so your password-added Google
account works. If your Unity account has **two-step verification** turned on, that sign-in can fail; then we use
another route. *(Untested until the first run.)* Secrets are safe in a public repo: GitHub never shows them, and
pull requests from other people can't read them.

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

## 6. Switch the Unity project to HDRP (once, at the PC)

1. Get the latest of this repo (in GitHub Desktop: **Fetch**, then **Pull**; or `git pull` in a terminal).
2. Open the project in Unity Hub. Unity installs the HDRP package (a few minutes).
3. Menu **Hatchfield → Switch to HDRP (max quality)**. Wait for "[hdrp] FINISHED" in the Console.
4. **Window → Rendering → HDRP Wizard**: every row green; press **Fix All** if not.
5. Press **Play**, take a screenshot, and commit and push the new files (`Assets/Settings/HDRP/`, the changed
   `ProjectSettings/` files and the `.meta` files).
Any "[hdrp] SKIPPED" line in the Console means that setting has another name in this HDRP version: send Claude
the Console text (no screenshots of keys).

