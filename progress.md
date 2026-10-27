# One More Time, Knight - Progress

Last updated: 2026-10-27
Status: Playable Android prototype built and smoke-tested on a physical phone. Submission polish and release packaging remain.

## Summary

One More Time, Knight is a 36-hour GameJamPlus entry: an audio-first blind-knight roguelite. The player turns with the phone gyroscope, taps to parry, and swipes to punish staggered enemies. The build is a code-only Unity URP project that constructs its camera, UI, audio, enemies, and game loop at runtime.

Current state:
- Full loop implemented: Title, Intro, three waves, interludes, death/retry, Conductor finale, Victory.
- Echo progression, persistent deaths/attempts, narrator captions, procedural audio, and 3D positional cues implemented.
- Android player settings configured and a current APK built and installed successfully.
- Device smoke test launched the app, rendered the title and gameplay UI, and reached Victory.
- Audio quality, gyro feel, latency, frame rate, and release signing are not yet formally validated.

## Current build

- APK: `Builds/Android/OneMoreTimeKnight.apk` (git-ignored)
- Build ID: `build_d0bf743bc99d`
- Result: Succeeded, 0 errors, 971 warnings
- Build time: 192.7 seconds
- Size: approximately 41 MB
- Package: `com.shoaib.onemoretimeknight`
- Version: `versionCode=1`, `versionName=1.0`
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

The APK metadata was verified with `aapt2 dump badging` and `apksigner verify --print-certs`.

## Device smoke test

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

## Implemented

Gameplay:
- Blind-knight loop: turn to face sound, tap to parry, swipe to kill staggered enemies.
- Three enemy variants: Stalker, Charger, Conjurer.
- Three waves with interlude healing and escalating narration.
- Perfect parry window, stagger state, whiff/block feedback, death recap, automatic retry after 4.8 seconds, and manual retry tap.
- Finale: turn toward the Conductor, then swipe.
- Echo progression: Foresight, Grace, Iron, Stillness, with persistent PlayerPrefs.
- Instant retry and attempt/death tracking.

Audio:
- Fully procedural audio synthesis at runtime; no imported audio assets.
- Windup cues, presence loops, drone/music, heartbeat, death, victory, narrator blips.
- 3D positional sources with distance rolloff and rear-facing low-pass filtering.
- Foresight perfect-window cue.
- Subtitle/narrator text for important cues.

UI:
- Runtime-built uGUI canvas, 1080x1920 reference resolution.
- Title, status line, subtitles, prompt, ring, damage/parry flashes.
- Portrait-only Android orientation.
- Title controls persist for 999 seconds or until the run starts.
- Prompt moved below the ring (`anchorMin/Max.y = 0.25`).

Robustness:
- New Input System touch, mouse, and keyboard fallbacks.
- Gyroscope NaN/Infinity guards before and after yaw normalization.
- `OnApplicationPause` handling for audio and `Time.timeScale` when not in the editor.
- Frame delta clamped to 0.05 seconds.

Editor QA completed earlier:
- Full Title to Victory autoplay with `OMTKPlaytest` (editor-only; excluded from Android builds).
- Clean PlayerPrefs run: 10.6 seconds at `Time.timeScale=2`.
- Echo test: four Echoes unlocked, `MaxHp=4`, death/retry/attempt tracking worked.
- Audio output RMS measured at 0.05352 at Victory.
- Injected mouse input drove Title to Playing and changed `PlayerYaw`.

## Known issues and risks

- Device audio has not been listened to on the actual phone; speaker/headphone mix and latency remain unverified.
- Gyro turning has not been measured quantitatively on device.
- No device FPS, thermal, battery, or long-session test has been run.
- Release signing is not configured. The APK is debug-signed and cannot be shipped to a store as-is.
- No AAB has been built.
- `ATTEMPT 2`/`HP 3/4` on the first device gameplay capture indicates persisted progress; clean-state behavior should be re-tested after `pm clear`.
- Editor screenshots are partially stale: `Assets/Screenshots/omtk_gameplay_portrait.png` currently contains a Retry intro, and `omtk_victory_portrait.png` predates the `THE HALL IS QUIET` status fix.
- The URP post-processing shaders stripped at build time mean Depth of Field and Panini passes are unavailable. The game does not depend on them, but the warnings should be cleaned up.
- The Unity AI/Sentis package produces unrelated shader warnings and adds build/package weight.
- Submission assets are incomplete: trailer, five final screenshots, README, pitch deck, and AAB are still pending.
- `OMTKPlaytest` is editor-only and is not present in the Android build; do not expect autoplay on device.

## Next steps

1. Clean device state with `adb shell pm clear com.shoaib.onemoretimeknight`, reinstall, and run a controlled Title -> Intro -> death -> retry -> final -> Victory test.
2. Listen to the build on the phone with headphones and speakers; tune cue levels, rear filtering, narrator clarity, and latency.
3. Measure gyro/touch feel and frame rate; check `adb shell dumpsys gfxinfo com.shoaib.onemoretimeknight` and a device profiler capture.
4. Decide whether to remove the unused Unity AI inference package and the unused URP post-processing features.
5. Configure release signing, build an AAB, and verify the signed artifact.
6. Replace stale screenshots, record a 60-second trailer, and finish README/pitch/submission assets.

## Commands

```bash
ADB=~/Unity/Hub/Editor/6000.6.7f1/Editor/Data/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb

# Build the current APK (Android target must already be active)
unity command build.run \
  --target Android \
  --outputPath Builds/Android/OneMoreTimeKnight.apk \
  --confirm true \
  --timeout 1800

# Check build status
unity command build.status

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

- `Assets/Scripts/OMTK/OMTKGame.cs` - state machine, UI, input, Echoes, death/retry, finale, lifecycle.
- `Assets/Scripts/OMTK/OMTKEnemy.cs` - enemy approach, windup, parry, stagger, death, spatial audio.
- `Assets/Scripts/OMTK/OMTKAudio.cs` - sources, ducking, voice cache, conductor hum.
- `Assets/Scripts/OMTK/AudioSynth.cs` - procedural audio generation.
- `Assets/Scripts/OMTK/OMTKConfig.cs` - constants, enemy variants, Echo metadata.
- `Assets/Scripts/OMTK/OMTKBootstrap.cs` - runtime bootstrap.
- `Assets/Scripts/OMTK/OMTKPlaytest.cs` - editor-only autoplay/QA driver.
- `Assets/Editor/OMTKBuild.cs` - PlayerSettings and Android/Linux build menu.
- `Assets/Scenes/SampleScene.unity` - only scene.
- `ProjectSettings/ProjectSettings.asset` - Android identity, SDK, orientation, input, version.
- `Docs/GameDesign.md` - jam-scoped design.
- `Docs/JamPrototypePlan.md` - earlier prototype plan.
- `Docs/AssetStoreInventory.md` / `.csv` - 733-package asset inventory.
- `AgentScripts/DumpMyAssets.cs` / `FetchDetails.cs` - asset inspection utilities.
- `Builds/Android/OneMoreTimeKnight.apk` - current debug-signed ARM64 APK.
- `Docs/screenshots/device-title.png` and `device-victory.png` - device smoke-test evidence.
