# One More Time, Knight - Progress

Last updated: 2026-10-28
Status: Tier 4 plus radar assist/pings implemented. Latest APK built; device verification is blocked by the disconnected phone.

## Summary

One More Time, Knight is a 36-hour GameJamPlus entry: an audio-first blind-knight roguelite. The player turns with the phone gyroscope, taps to parry, and swipes to punish staggered enemies. The build is a code-only Unity URP project that constructs its camera, UI, audio, enemies, and game loop at runtime.

Current state:
- Full loop implemented: Title, Intro, three waves, interludes, interactive death rehearsal, three-beat Conductor finale, Victory.
- Permanent Codex/Learned knowledge, run-scoped bargains, narrator captions, procedural audio, and 3D positional cues implemented.
- Tier 4 headless verification: Linux build passed with 0 errors and 974 known warnings; suite `197/197`; smoke reached Victory at 3 HP in `31.01 s`.
- Android player settings configured. Latest radar-ping APK built and statically verified; radar title/death visuals were device-tested on an earlier build. The latest APK is not installed because the phone is disconnected.
- Radar assist is optional and off by default: bearing dial/blips plus proximity-pitched pings, hidden during calibration.
- Audio quality, gyro feel, latency, frame rate, TTS timing, haptics, radar ping mix, and release signing are not yet formally validated.

## Current verified build

- Binary: `Builds/Linux/OneMoreTimeKnight.x86_64`
- Build ID: `build_31e1d5e36c33`
- Result: Succeeded, 0 errors, 974 known warnings
- Size: 137,115,660 bytes
- Suite: `197/197` checks
- Smoke: Victory at 3 HP in 31.01 seconds

## Current Android APK

- APK: `Builds/Android/OneMoreTimeKnight.apk` (git-ignored)
- Built: `2026-10-28 07:03`
- Result: Succeeded, 0 errors
- Build time: 66.4 seconds
- Size: 42,920,995 bytes (approximately 40.9 MiB)
- SHA-256: `c46faa199084f3ea94a8b2e1fddd22f9fa263aa60ff5e15a7cfbca3094e88535`
- Package: `com.shoaib.onemoretimeknight`
- Version: `versionCode=1`, `versionName=0.1.0`
- SDK: `minSdk=31` (Android 12), `targetSdk=36`
- ABI: `arm64-v8a` only
- Orientation: portrait
- Launch activity: `com.unity3d.player.UnityPlayerGameActivity`
- Signing: Android Debug certificate (`CN=Android Debug`); not store-ready.

Build settings:
- Unity `6000.6.7f1`, URP, IL2CPP.
- New Input System only (`activeInputHandler: 1`).
- Scene: `Assets/Scenes/SampleScene.unity`.
- Development build and script debugging are disabled.
- Android target architectures: ARM64 (`AndroidTargetArchitectures: 2`).

The APK metadata was verified with `aapt2 dump badging` and `apksigner verify --print-certs`; it is debug-signed.

Changes in this build:
- Narrator-independent `1.25 s` parry cooldown, immediate victory tension cleanup, and radar blip cleanup.
- Optional radar assist: bearing dial/blips plus gentle descending pings, panned at the target; winding enemy first, otherwise nearest. Pings are gated to `Playing`, silenced while narration is active, and disabled with radar assist.
- IL2CPP output was checked for `RadarPing`, `UpdateRadarAudio`, and `IsSpeechActive`, confirming the latest source is in the APK.
- Expanded runtime suite to `197/197`: Grace boundary, cooldown modes, Conjurer/dual-token scheduling, onboarding bearings, narration wait, death rehearsal failures/rejection/skip, fatal cue filtering, timeout and final-defense paths, and reset/repeated-death invariants.
- The latest APK is not installed because the phone is disconnected; the radar visuals were verified on an earlier device build, but these pings have not been heard on device.

## Device smoke test (historical, pre-Tier 3 build)

This section records an earlier device smoke test against a pre-Tier 3 APK. It is not evidence for the latest radar-ping APK, which has not been installed.

Device:
- Motorola `moto g34 5G` (`ZA222JRC3B`)
- Android 15 / API 35
- 720x1600 display, 280 dpi
- `arm64-v8a`
- Gyroscope present (`icm4x6xa Gyroscope`)

Install and launch:
- `adb install -r Builds/Android/OneMoreTimeKnight.apk` succeeded.
- App launched without a startup crash.
- Title screen, circular ring, CTA, and control caption rendered correctly at 720x1600.
- A tap started a run; HUD and wave state rendered.
- A Victory screen was observed: `THE ECHO ENDS`, `THE HALL IS QUIET`, and `TAP TO BEGIN AGAIN`.
- The device was not in a clean progress state: the first gameplay capture showed `ATTEMPT 2` and `HP 3/4`, indicating persisted PlayerPrefs/prior deaths. This was not treated as a blocker.
- The Victory observation was not from an automated device test. Inputs were adb taps and/or manual interaction, so it is a smoke-test result, not a deterministic regression test.

Screenshots:
![Device title](Docs/screenshots/device-title.png)
![Device victory](Docs/screenshots/device-victory.png)

Logs:
- No Unity startup exception was seen in the captured log tail.
- URP reported that Gaussian/Bokeh Depth of Field and Panini Projection shaders were stripped from the build; those post-processing passes will not execute.
- The Unity AI inference/Sentis package emitted many shader warnings. The package is not used by the game; it can be removed later to reduce build noise and size.
- Logcat showed frame-late entries during `screencap`; these are not a substitute for an FPS measurement.

## Radar device verification (earlier build)

- Radar assist was enabled manually and captured on the Motorola device before the latest ping build.
- Title capture showed `HDG 337°` with the wave-1 Stalker blip at 3 o'clock, matching azimuth `62°` against player yaw `337°`.
- Death capture showed the red fatal bearing blip at the right edge with `HP 0/4` and `ATTEMPT 2`; HUD health is text-only, so there are no heart sprites.
- The death capture was clean after notifications cleared: `/tmp/radar_play_clean.png`. The title capture still had a Discord overlay: `/tmp/radar_title2.png`.
- This verifies radar rendering, bearing mapping, and the fatal marker. Finale/calibration visuals are implemented but still need device captures; the latest ping audio is unverified.

## Implemented

Gameplay:
- Blind-knight loop: turn to face sound, tap to parry, swipe to kill staggered enemies.
- Three enemy variants: Stalker, Charger, Conjurer.
- Three waves with interlude healing and escalating narration.
- Perfect parry window, stagger state, whiff/block feedback, death recap, automatic retry after 4.8 seconds, and manual retry tap.
- Death replay of the fatal windup cue at 0.62 pitch with low-pass filtering, after the death announcement.
- Finale: turn toward the Conductor, then swipe.
- Permanent Codex discovery and per-variant mastery; no permanent stat growth or HP inflation.
- Run-only bargains: Foresight, Grace, Iron, Stillness; cleared on retry/victory.
- Interactive death rehearsal with alignment tap, bargain acceptance/rejection, bounded retry, and accessibility skip.
- Three-beat Conductor finale with true/decoy harvest, HP restart at one heart, final defense, and sheathe victory sequence.
- Instant retry and attempt/death tracking.

Audio:
- Fully procedural audio synthesis at runtime; no imported audio assets.
- Windup cues, presence loops, drone/music, heartbeat, death, victory, narrator blips.
- 3D positional sources with distance rolloff and rear-facing low-pass filtering.
- Foresight perfect-window cue.
- Optional radar pings: low descending 3D ping positioned at the selected enemy, with proximity-based timing, volume, and pitch; winding enemy takes priority, otherwise nearest.
- Subtitle/narrator text for important cues.

UI:
- Runtime-built uGUI canvas, 1080x1920 reference resolution.
- Title, status line, subtitles, prompt, ring, damage/parry flashes.
- Portrait-only Android orientation.
- Title controls persist for 999 seconds or until the run starts.
- Prompt moved below the ring (`anchorMin/Max.y = 0.25`).
- Status text at 32 and prompt text at 36 for stronger phone readability.
- Radar assist is one of five options: bearing dial, four-colour blip set, heading readout, death/finale markers, and hidden calibration state. Radar assist defaults off.

Robustness:
- New Input System touch, mouse, and keyboard fallbacks.
- Gyroscope NaN/Infinity guards before and after yaw normalization.
- `OnApplicationPause` handling for audio and `Time.timeScale` when not in the editor.
- Frame delta clamped to 0.05 seconds.

Editor QA completed earlier:
- Full Title to Victory autoplay with `OMTKPlaytest` (editor-only; excluded from Android builds).
- Clean PlayerPrefs run: 10.6 seconds at `Time.timeScale=2`.
- Historical Echo test: four legacy Echoes unlocked and `MaxHp=4` before the redesign retired permanent power.
- Audio output RMS measured at 0.05352 at Victory.
- Injected mouse input drove Title to Playing and changed `PlayerYaw`.

## Known issues and risks

- Device audio has not been listened to on the actual phone; speaker/headphone mix and latency remain unverified.
- The latest radar ping build has not been installed; ping level, pitch, panning, cadence, and narration ducking are unheard.
- Gyro turning has not been measured quantitatively on device.
- No device FPS, thermal, battery, or long-session test has been run.
- Release signing is not configured. The APK is debug-signed and cannot be shipped to a store as-is.
- No AAB has been built.
- The historical `ATTEMPT 2`/`HP 3/4` device capture predates the fixed three-heart redesign; clean-state behavior must be re-tested on the current APK after `pm clear`.
- Five editor screenshots were refreshed from the current build: title, intro, interlude, wave three, and victory under `Assets/Screenshots/`. The gameplay shot is intentionally minimal because the game is audio-first; it shows the ring, HUD, and `LISTEN` prompt.
- Radar screenshots exist only in `/tmp`; a clean title/gameplay/finale set still needs capture. `/tmp/radar_title2.png` has a Discord overlay; `/tmp/radar_play_clean.png` is clean but is a death state.
- The URP post-processing shaders stripped at build time mean Depth of Field and Panini passes are unavailable. The game does not depend on them, but the warnings should be cleaned up.
- The Unity AI/Sentis package produces unrelated shader warnings and adds build/package weight.
- Submission assets: five editor screenshots and `README.md` are present; trailer, pitch deck, and AAB are still pending.
- The phone is not connected, so the latest radar-ping build has not been installed. The last device build verified radar visuals but predates the pings.
- `OMTKPlaytest` is editor-only and is not present in the Android build; do not expect autoplay on device.

## Next steps

1. Reconnect the phone (`ZA222JRC3B`) with USB debugging unlocked; confirm `adb devices -l`.
2. Install the latest APK and clean state with `adb shell pm clear com.shoaib.onemoretimeknight`.
3. Run a radar-focused device pass: assist off/on, wave 1 and wave 3 multi-enemy blips, Finale conductor, fatal death marker, calibration hiding, and ping panning/cadence. Listen on headphones and the phone speaker.
4. Run the remaining controlled device pass: death rehearsal, bargain accept/reject, Codex reset, three-beat Finale, and Victory.
5. Measure gyro/touch feel and frame rate; check `adb shell dumpsys gfxinfo com.shoaib.onemoretimeknight` and a device profiler capture.
6. Capture clean radar title/gameplay/finale screenshots; then configure release signing, build an AAB, and finish submission assets.

## Commands

```bash
ADB=~/Unity/Hub/Editor/6000.6.7f1/Editor/Data/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb

# Close the live Editor first, then build the current APK.
unity build --target Android \
  --execute-method OMTK.Editor.OMTKBuild.BuildAndroid \
  --allow-dirty-build --no-tail \
  --log-file /tmp/omtk_android_build.log

# Inspect a completed build
rg -n "Build Finished|Android build written|error CS" /tmp/omtk_android_build.log

# Install and launch
$ADB install -r Builds/Android/OneMoreTimeKnight.apk
$ADB shell am force-stop com.shoaib.onemoretimeknight
$ADB shell am start -n com.shoaib.onemoretimeknight/com.unity3d.player.UnityPlayerGameActivity

# Runtime logs
$ADB logcat -s Unity

# Clean local game progress
$ADB shell pm clear com.shoaib.onemoretimeknight
```

## Key files

- `Assets/Scripts/OMTK/OMTKGame.cs` - state machine, UI, input, Codex/bargains, death rehearsal, Finale, lifecycle.
- `Assets/Scripts/OMTK/OMTKEnemy.cs` - enemy approach, windup, parry, stagger, death, spatial audio.
- `Assets/Scripts/OMTK/OMTKAudio.cs` - sources, ducking, voice cache, conductor hum.
- `Assets/Scripts/OMTK/AudioSynth.cs` - procedural audio generation.
- `Assets/Scripts/OMTK/OMTKConfig.cs` - constants, enemy variants, Codex/bargain metadata.
- `Assets/Scripts/OMTK/OMTKBootstrap.cs` - runtime bootstrap.
- `Assets/Scripts/OMTK/OMTKPlaytest.cs` - editor-only autoplay/QA driver.
- `Assets/Editor/OMTKBuild.cs` - PlayerSettings and Android/Linux build menu.
- `Assets/Scenes/SampleScene.unity` - only scene.
- `ProjectSettings/ProjectSettings.asset` - Android identity, SDK, orientation, input, version.
- `Docs/GameDesign.md` - jam-scoped design.
- `Docs/JamPrototypePlan.md` - earlier prototype plan.
- `Docs/AssetStoreInventory.md` / `.csv` - 733-package asset inventory.
- `AgentScripts/DumpMyAssets.cs` / `FetchDetails.cs` - asset inspection utilities.
- `Builds/Android/OneMoreTimeKnight.apk` - latest radar-ping debug-signed ARM64 APK; not installed.
- `/tmp/omtk_radar_ping_build3.log` - successful latest Android build, including IL2CPP generation.
- `README.md` - controls, radar assist, accessibility, build/run notes.
- `Docs/screenshots/device-title.png` and `device-victory.png` - historical device smoke-test evidence.
- `/tmp/radar_title2.png`, `/tmp/radar_play_clean.png`, `/tmp/radar_blip_crop.png` - radar device captures; latest ping build not represented.
