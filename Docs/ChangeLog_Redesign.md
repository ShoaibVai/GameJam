# OMTK Redesign Change Log

This log records implementation work against the acoustic redesign specification. Build results are reported only for checks that were actually executed.

## Tier 1.1 — Directional Parry Grades and Tinnitus

- **Status:** Implemented; Tier 1 runtime suite passing.
- **Behavior implemented:**
  - Parry attempts are graded as perfect, standard, or glancing using the player's facing and the remaining windup.
  - Perfect parries require a valid angle, a valid timing window, and a stable resonance lock.
  - Standard parries negate damage and shorten the next attack cooldown by 30%.
  - Glancing deflects (35°–40°) push the enemy away instead of staggering it.
  - Off-axis attempts beyond 40° fail without cancelling the enemy attack.
  - Premature taps trigger guard lockout and the tinnitus low-pass effect.
- **Files changed:** `Assets/Scripts/OMTK/OMTKConfig.cs`, `Assets/Scripts/OMTK/OMTKEnemy.cs`, `Assets/Scripts/OMTK/OMTKGame.cs`, `Assets/Scripts/OMTK/OMTKAudio.cs`, `Assets/Scripts/OMTK/AudioSynth.cs`, `Assets/Scripts/OMTK/OMTKPlaytest.cs`.
- **Configuration added:**
  - `PerfectParryAngle = 18f`
  - `GracePerfectParryAngle = 25f`
  - `StandardParryAngle = 35f`
  - `GlancingDeflectAngle = 40f`
  - `PerfectParryWindow = 0.14f`
  - `GracePerfectParryWindow = 0.22f`
  - `StandardParryWindow = 0.35f`
  - `StandardParryCooldownScale = 0.7f`
  - `GuardLockoutDuration = 0.45f`
  - `TinnitusFrequency = 900f`
  - `TinnitusDuration = 0.8f`
  - `PerfectStaggerDuration = 1.2f`
- **Playtests:** Tier 1 suite covers 18°/35°/40° boundaries, perfect-window timing, lock-required perfects, and phrase stabilization.
- **Build/test result:** Compile check and Linux build passed; Tier 1 runtime suite passed 56/56 tests; headless smoke run reached Victory at 3 HP.
- **Deviations/deferred work:** Automated boundary coverage is now implemented in `OMTKPlaytest`; device feel remains to be validated.

## Tier 1.2 — Separate Tap Parry from Swipe Finisher

- **Status:** Implemented; Tier 1 runtime suite passing.
- **Behavior implemented:**
  - Tapping while an enemy is staggered no longer kills it. The tap shoves the enemy back to approximately 7.4 m and resets its posture.
  - Swipe remains the only finisher, and the target must be within the 35° kill cone.
  - Fast horizontal swipes and drag-to-turn are separated with dead-zone and velocity thresholds.
  - Yaw accumulation is frozen while an enemy is staggered or after a gesture is classified as a swipe, preventing a slash from changing its own attack angle.
- **Files changed:** `Assets/Scripts/OMTK/OMTKGame.cs`, `Assets/Scripts/OMTK/OMTKEnemy.cs`, `Assets/Scripts/OMTK/OMTKConfig.cs`.
- **Configuration added:**
  - `TouchDeadZone = 8f`
  - `SwipeDistanceThreshold = 90f`
  - `SwipeVelocityThreshold = 800f`
- **Playtests:** Tier 1 suite covers tap shove, shove distance, on-axis slash, off-axis slash rejection, and swipe yaw stability.
- **Build/test result:** Compile check and Linux build passed; Tier 1 runtime suite passed 56/56 tests; headless smoke run reached Victory at 3 HP.
- **Deviations/deferred work:** Automated gesture separation is now implemented in `OMTKPlaytest`; device touch feel remains to be validated.

## Tier 1.3 — Resonance Lock and Lock-In Audio

- **Status:** Implemented; Tier 1 runtime suite passing.
- **Behavior implemented:**
  - Each living enemy tracks whether the player has held facing within 35° for 0.3 seconds.
  - Acquiring a lock plays a short, enemy-specific ring-in tone at the enemy's spatial position.
  - Leaving the 35° cone immediately drops the lock and resets its timer.
  - Perfect parries now require a stable lock in addition to the perfect angle and timing window. Without a lock, a valid in-cone parry resolves as standard.
  - Lock tones use a new short chime layered over the existing presence and windup language; the existing enemy identity sounds are unchanged.
- **Files changed:** `Assets/Scripts/OMTK/OMTKConfig.cs`, `Assets/Scripts/OMTK/OMTKEnemy.cs`, `Assets/Scripts/OMTK/OMTKGame.cs`, `Assets/Scripts/OMTK/OMTKAudio.cs`, `Assets/Scripts/OMTK/AudioSynth.cs`.
- **Configuration added:**
  - `ResonanceLockDuration = 0.3f`
  - Per-enemy lock clips derived from `EnemyVariant`.
- **Playtests:** Tier 1 suite covers lock acquisition at 0.3s, lock drop outside the cone, and perfect-parry enforcement without a lock.
- **Build/test result:** Compile check and Linux build passed; Tier 1 runtime suite passed 56/56 tests; headless smoke run reached Victory at 3 HP.
- **Deviations/deferred work:** Lock behavior is covered headlessly; device audio timing remains to be validated.

## Tier 1.4 — Opt-In Radar Assistance

- **Status:** Implemented; Tier 1 runtime suite passing.
- **Behavior implemented:**
  - Directional radar now defaults to off and is persisted with `PlayerPrefs` key `OMTK_RadarAssist`.
  - When off, the radar dial, cardinal labels, heading readout, enemy blips, attack-timing blips, and radar pings are not drawn or played. The non-directional resonance ring remains visible.
  - When on, the existing directional radar and radar pings are restored.
  - The title options menu now supports row navigation: tap changes the selected row, swipe moves to the next row, and two-finger tap closes the menu.
  - Added a persisted `RADAR ASSIST: ON/OFF` row with subtitle and narrator feedback.
  - Removed the off-axis finisher subtitle that disclosed that the opening was behind the player.
- **Files changed:** `Assets/Scripts/OMTK/OMTKConfig.cs`, `Assets/Scripts/OMTK/OMTKGame.cs`.
- **Configuration added:**
  - `OMTKConfig.RadarAssist = false`
  - `RadarAssistKey = "OMTK_RadarAssist"`
  - Options rows now include narrator, haptics, and radar assist.
- **Playtests:** Tier 1 suite covers radar-off invisibility/readout hidden, persistence/reload, non-directional echo ring, and calibration suppression.
- **Build/test result:** Compile check and Linux build passed; Tier 1 runtime suite passed 56/56 tests; headless smoke run reached Victory at 3 HP.
- **Deviations/deferred work:** The options menu uses tap-to-change and swipe-to-navigate rather than the previous fixed tap/swipe actions. This is required to expose more than two settings without adding new gestures. Radar behavior is covered headlessly; visual inspection remains pending.

## Tier 1.5 — Wave 0 Calibration and Scripted First Death

- **Status:** Implemented; Tier 1 runtime suite passing.
- **Behavior implemented:**
  - Attempt 1 now routes from the intro into an interactive calibration state before Wave 1.
  - Calibration emits a spatial tuning tone from 150° at 3.2 m. Its low-pass cutoff rises from 600 Hz toward 5000 Hz as the player faces it, reaching 5000 Hz inside the 15° alignment window.
  - The exact instructional line is spoken and subtitled: “Dull means behind. Turn until the blade sings, then tap.”
  - Tapping inside 15° starts Wave 1. Tapping outside the window repeats the turn instruction.
  - Slow or missed orientation is handled with repeated nudges and an explicit 10.5-second fallback that starts Wave 1 rather than trapping the player.
  - Directional radar is hidden during calibration so the heading readout cannot replace listening.
  - Attempt 1 Wave 1 uses a deterministic tutorial Stalker at the calibration bearing. It reaches attack range, plays the normal Stalker windup identity, and forces a fatal hit regardless of parry input or radar state.
  - The tutorial death replays the fatal cue through the existing death sequence and narrates “Turn toward the sound.” after the replay.
  - The tutorial death does not unlock a permanent Echo, preventing the scripted failure from granting a persistent combat advantage.
- **Files changed:** `Assets/Scripts/OMTK/OMTKConfig.cs`, `Assets/Scripts/OMTK/AudioSynth.cs`, `Assets/Scripts/OMTK/OMTKAudio.cs`, `Assets/Scripts/OMTK/OMTKEnemy.cs`, `Assets/Scripts/OMTK/OMTKGame.cs`.
- **Configuration added:**
  - `CalibrationAzimuth = 150f`
  - `CalibrationAlignAngle = 15f`
  - `CalibrationToneDistance = 3.2f`
  - `CalibrationFallbackDuration = 10.5f`
  - `CalibrationNudgeInterval = 3.4f`
  - `TutorialFirstDeathDelay = 3f`
  - `TutorialFirstDeathWindup = 0.85f`
- **Playtests:** Tier 1 suite covers calibration misalignment rejection, aligned acceptance, fallback exit, scripted tutorial windup, unparryable blow, deterministic first death, and no Echo grant.
- **Build/test result:** Compile check and Linux build passed; Tier 1 runtime suite passed 56/56 tests; headless smoke run reached Victory at 3 HP.
- **Deviations/deferred work:** The death sequence remains the existing passive replay/auto-retry until Tier 3 replaces it with the interactive Trace the Blow rehearsal. The temporary Echo unlock is suppressed specifically for the tutorial death so Tier 1 does not grant permanent combat power from the scripted failure.

## Tier 1.6 — Yaw Hygiene, Accessibility Gestures, and Volume

- **Status:** Implemented; Tier 1 runtime suite passing.
- **Behavior implemented:**
  - `PlayerYaw` recenters to 0° on `StartRun`, `Retry`, `BeginWave`, and calibration start.
  - Every recenter plays a distinct centered two-tone orientation cue so the new forward reference is audible.
  - Two-finger tap repeats the last stored acoustic cue and speaks the current heading. If no cue exists, it plays a tick and speaks the heading.
  - Two-finger double-tap recenters yaw, plays the recenter cue, and speaks current HP and wave.
  - Single-tap accessibility actions are delayed by a 0.34-second double-tap window so a double-tap does not also fire the single-tap action.
  - Two-finger input cancels the active pointer gesture, preventing accidental parries or slashes.
  - While the options menu is open, two-finger tap closes it instead of triggering accessibility actions.
  - Keyboard `H` repeats the last cue and heading; keyboard `R` recenters and reports HP/wave for desktop testing.
  - Added a persisted `VOLUME` options row cycling in 25% steps and applying `AudioListener.volume`. Speech is suppressed at zero volume.
- **Files changed:** `Assets/Scripts/OMTK/OMTKConfig.cs`, `Assets/Scripts/OMTK/AudioSynth.cs`, `Assets/Scripts/OMTK/OMTKAudio.cs`, `Assets/Scripts/OMTK/OMTKGame.cs`.
- **Configuration added:**
  - `VolumeKey = "OMTK_Volume"` with a default of 1.0 and 0.25 steps.
  - `TwoFingerTapWindow = 0.34f`
  - `TwoFingerMaxTapDuration = 0.55f`
  - `OptionsCount = 4`
- **Playtests:** Tier 1 suite covers recenter transitions/clip, heading repeat without recenter, double-tap recenter, HP/wave report, volume persistence/reload, zero-volume wrap, and speech suppression.
- **Build/test result:** Compile check and Linux build passed; Tier 1 runtime suite passed 56/56 tests; headless smoke run reached Victory at 3 HP.
- **Deviations/deferred work:** Two-finger tap no longer opens the title options menu; the documented single-finger hold remains the options entry point. This avoids conflicting with the required accessibility gesture. Device audio, haptics, and TTS behavior have not been verified in this environment.

## Tier 1 closure — remove permanent combat advantages

- **Status:** Implemented; Tier 1 runtime suite passing.
- **Behavior implemented:**
  - Legacy Echo unlocks no longer alter perfect-window width, perfect-angle width, enemy windup duration, or maximum HP.
  - Maximum HP is fixed at `3`.
  - `HasEcho` always returns false; legacy `OMTK_EchoMask` is deleted on load and no new Echo bonuses are granted.
  - Deaths still increment for statistics and best-wave tracking, but repeated deaths grant no combat advantage.
- **Files changed:** `Assets/Scripts/OMTK/OMTKGame.cs`.
- **Playtests:** Tier 1 suite verifies fixed HP, ignored and cleared legacy Echo mask, no new Echo unlock, and no legacy combat effect.
- **Build/test result:** Compile check and Linux build passed; Tier 1 runtime suite passed 56/56 tests; headless smoke run reached Victory at 3 HP.
- **Deviations/deferred work:** Codex discovery/mastery and the interactive death rehearsal remain scheduled for Tier 3. Echo unlock narration is currently suppressed so no unusable bonus is advertised.

## Tier 1 verification

- **Status:** Passing.
- **Automated suite:** `-omtkPlaytestSuite` passed 56/56 checks covering parry grades, resonance lock, premature tap, shove/slash separation, radar privacy, calibration, tutorial death, progression invariants, recenter, volume, and accessibility gestures.
- **Smoke run:** `-omtkPlaytest` completed Wave 3 and reached Victory at 3 HP in 16.4 seconds with exit code 0.
- **Environment:** Linux x86_64 headless build. Device audio, haptics, and TTS remain unverified.
