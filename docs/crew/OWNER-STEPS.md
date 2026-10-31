# Things only Hatch can do

One step at a time. Never paste a key, token, password or licence file into a chat.

## 1. Make the harness private (and keep its checks free)

1. Open github.com/treverhatch-glitch/3d-deep-sim-studio-harness → **Settings** → scroll to **Danger Zone** →
   **Change repository visibility** → **Make private** → confirm.
2. Its checks use about 52 minutes per push. Private repos get 2,000 free minutes a month, about a week at our
   pace. Two free ways round it (tell Claude which):
   - **A runner on your PC** (recommended for the private harness only): Settings → **Actions** →
     **Runners** → **New self-hosted runner** → **Windows** → run the commands GitHub shows, in PowerShell,
     in a new folder such as `C:\actions-runner`. The page shows a one-time registration token: type it
     there, not in a chat. Then Claude switches the harness checks to that runner.
   - **Fewer checks per push:** Claude trims the harness CI to the essentials.
3. The two old public copies of the harness, `hatchfield-ai-studio-template` and `ANTI-GRAV` (version 2.1):
   make each private (same Danger Zone button) or delete it. First check nobody, including you, has work in
   them that isn't pushed elsewhere.

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

The bots run on your PC, so keep the private key where they can't read it, such as a USB stick:

1. Plug in the USB stick (say it is drive `E:`).
2. In PowerShell, in the harness folder:
   `$env:STUDIO_OWNER_KEY = "E:\studio-owner"; npm run approve -- keygen`
3. Commit and push only the public half: `git add studio/keys/owner-ed25519.pub.pem`, then commit and push.
4. Unplug the stick. Plug it in only when you approve something (`npm run approve -- protected --note "why"`).

## 4. Branch protection (your answer: yes)

1. Repo → **Settings** → **Rules** → **Rulesets** → **New branch ruleset**.
2. Target: the default branch (`main`). Turn on **Require a pull request before merging**, **Require status
   checks to pass** (pick the CI checks), **Block force pushes** and **Restrict deletions**. Save.
3. Free for public repos. **A private repo needs a paid GitHub plan for this**, so for the private harness it
   costs money: your call.

## 5. Making `hatchfield-game` public

Claude scans its whole history first. It reports any keys, private notes or links, and every file and
commit that would become visible. You get that list and decide. Nothing changes before your OK.

## 6. Graphics pipeline

URP at maximum settings plays in the browser and on PC. HDRP has the best lighting but builds for PC only.
Tell Claude which.
