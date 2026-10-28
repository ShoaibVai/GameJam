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

## Tier 2 — Enemy Acoustic Ecology, Attention Management, and Encounter Variation

- **Status:** Implemented; Tier 2 runtime suite passing.
- **Behavior implemented:**
  - Stalkers oscillate their approach azimuth through approximately ±32° and freeze that bearing the instant their real windup begins. The existing `170 Hz → 720 Hz` windup identity is unchanged.
  - Charger presence pulse tempo now follows the deterministic distance curve: approximately `1.0 Hz` at `10 m`, rising linearly to `5.5 Hz` at `2.35 m`. The Charger pulse and windup identities are preserved.
  - A glancing Charger deflect now moves it to approximately `4.5 m` at `Azimuth + 180°`, then starts a new approach from behind. Positional audio and combat state use the same bearing.
  - Conjurers maintain a secondary phantom source at `Azimuth ± 90°` during approach. The phantom decoy windup starts `0.4 s` before the real windup and contains only the `980 Hz` upper-tone component; the real Conjurer tell is unchanged.
  - Parrying toward the Conjurer decoy is rejected as an off-axis defense and triggers the Tinnitus lockout.
  - The scheduler now tracks `primaryToken` and `offbeatToken` independently. On Wave 3 or Attempt 3+, a primary windup schedules an offbeat windup `0.55 s` later when a second enemy is at attack range.
  - A perfect parry of the primary while the offbeat is winding applies a `0.4 s` dip to `Time.timeScale = 0.65`, plays a spatial ping from the offbeat, and pitches the Drone and Tension beds down. The time scale and pitch always restore on completion or state change.
  - Enemy interruption, death, token cleanup, and state transitions clear or invalidate scheduled attacks; interrupted winding enemies recover instead of later striking without a token.
  - With two active enemies, the unattended enemy's attack and presence low-pass cutoff is lowered toward `45%` of its attended clarity while its spatial bearing and identity remain intact. Clarity returns as the player turns toward it.
  - Attempt 1 preserves the fixed `62°`, `-74°`, and `142° / -38°` onboarding bearings. Attempt 2+ uses per-enemy bearing jitter, enforces at least `85°` separation, adds Wave 2/Wave 3 pressure from Attempt 3, and scales approach speed gradually.
  - One Conductor edit is selected from Attempt 2 onward: swap a wave enemy type, hasten the approach, or add an extra attacker. `Retry` narrates one short hint without revealing bearings or exact attack timing.
  - Attack cooldown is standardized at `1.25 s`; wave-start cooldown is `1.8 s` after narration or startup speech finishes. The scheduler waits for `Audio.IsSpeechActive` to clear and does not wait indefinitely in OFF mode.
  - The previously unused `AudioSynth.Tension()` layer now plays during Wave 3 and the Finale with a controlled volume crossfade; it is disabled on death, victory, retry, and early waves.
- **Files changed:** `Assets/Scripts/OMTK/OMTKConfig.cs`, `Assets/Scripts/OMTK/AudioSynth.cs`, `Assets/Scripts/OMTK/OMTKAudio.cs`, `Assets/Scripts/OMTK/OMTKSystemTts.cs`, `Assets/Scripts/OMTK/OMTKEnemy.cs`, `Assets/Scripts/OMTK/OMTKGame.cs`, `Assets/Scripts/OMTK/OMTKPlaytest.cs`.
- **Configuration added:**
  - `StalkerOscillation = 32f`
  - `ChargerFarDistance = 10f`, `ChargerNearDistance = 2.35f`
  - `ChargerFarTempo = 1f`, `ChargerNearTempo = 5.5f`
  - `ChargerOvershootDistance = 4.5f`
  - `ConjurerDecoyLead = 0.4f`
  - `OffbeatWindupDelay = 0.55f`
  - `OffbeatTimeScale = 0.65f`, `OffbeatTimeDipDuration = 0.4f`
  - `UnattendedCutoffScale = 0.45f`
  - `AttackCooldown = 1.25f`, `WaveStartCooldown = 1.8f`
  - `MinEnemySeparation = 85f`
  - `StalkerBearingJitter = 32f`, `ChargerBearingJitter = 40f`, `ConjurerBearingJitter = 34f`
- **Playtests:** Tier 2 adds 37 headless checks for Charger tempo, Stalker oscillation and windup lock, Charger overshoot, Conjurer decoy lifecycle and Tinnitus rejection, dual-token retention, time-dip start/restore, attention targets, fixed/randomized bearings, pair separation, Conductor edits, cooldown normalization, and Tension state.
- **Build/test result:** Compile check and Linux build passed; the expanded runtime suite passed 93/93 checks; headless smoke run reached Victory at 3 HP in 17.4 seconds with exit code 0.
- **Deviations/deferred work:** Perceived mix clarity under headphones, Android TTS timing, haptics, and the Android build remain unverified in this environment. The interactive death rehearsal and split progression remain Tier 3 work.

## Tier 2 verification

- **Status:** Passing.
- **Automated suite:** `-omtkPlaytestSuite` passed 93/93 checks, including Tier 1 regression coverage and 37 Tier 2 checks.
- **Smoke run:** `-omtkPlaytest` completed Wave 3 and reached Victory at 3 HP in 17.4 seconds with exit code 0.
- **Build:** Linux x86_64 headless build passed, `137,918,972` bytes.
- **Environment:** Linux headless. Device audio, haptics, and TTS remain unverified.

## Tier 3 — Trace the Blow, Split Progression, and the Conductor Betrayal

- **Status:** Implemented; Tier 3 runtime suite passing.
- **Behavior implemented:**
  - Split progression into permanent Codex/Learned masks and per-run bargain/resonance state. `EchoMask` is now a compatibility alias for Codex discovery; legacy Echo bonuses and `UnlockNextEcho()` remain retired. Maximum HP stays fixed at `3`.
  - Added the Trace the Blow death rehearsal. The Conductor note lands at `1.05 s`; the fatal windup replays at `2.1 s` and every `1.8 s` afterward at `0.62x` pitch through a `1600 Hz` low-pass. The replay window opens `0.35 s` after each cue and lasts `0.35 s`; alignment tolerance is `25°`, fallback resolution is `4.8 s`, and the retry delay is bounded at `5 s`.
  - A correctly aligned tap during a replay completes the rehearsal, records the fatal variant in the Codex, and awards one `0.85x` windup bonus for the next attempt. The bonus expires on the following attempt.
  - A bargain can be accepted with a swipe up or a second tap, but acceptance does not skip the aligned tap and does not resolve death. A correctly timed aligned tap rejects the offered bargain. `Escape` or a two-finger tap skips the rehearsal for accessibility.
  - Added three training-chime charges per attempt for discovered but unmastered enemies. Perfect parries without a training assist mark mastery and silence future chimes for that variant.
  - Added run-scoped bargains: Grace widens perfect window/angle, Stillness slows enemy windups, Foresight adds an early chime, and Iron absorbs one hit. The run mask, resonance, bonuses, and shield clear on victory, retry, or a new run.
  - Rebuilt the Finale as three beats: the Conductor speaks in front and drifts behind; the harvest alternates true and decoy bearings with deceptive prompts and strict windows; a final aligned defense must be followed by a swipe toward the true bearing. False prompts never move the true source.
  - Off-axis, decoy, premature, and timeout responses damage HP through the shared Finale penalty path. HP reaching zero restarts only the Finale at `1 HP` with “Not yet. Again.” without leaving a stale threat resolving after the restart.
  - Victory mutes Drone/Tension/Hum, waits `1.5 s`, plays the sheathe cue, waits `0.45 s`, then enters Victory. The final narrator line is spatial at `180°`; run bargains and resonance clear while Codex and mastery persist.
  - Expanded the options menu to five rows and added `RESET PROGRESS`, clearing Codex, mastery, best wave, deaths, and legacy Echo keys.
- **Files changed:** `Assets/Scripts/OMTK/OMTKConfig.cs`, `Assets/Scripts/OMTK/AudioSynth.cs`, `Assets/Scripts/OMTK/OMTKAudio.cs`, `Assets/Scripts/OMTK/OMTKEnemy.cs`, `Assets/Scripts/OMTK/OMTKGame.cs`, `Assets/Scripts/OMTK/OMTKPlaytest.cs`.
- **Configuration added:**
  - `DeathNoteTime = 1.05f`, `DeathCueFirstTime = 2.1f`, `DeathCueInterval = 1.8f`
  - `DeathCuePitch = 0.62f`, `DeathCueLowPass = 1600f`
  - `DeathRehearsalAngle = 25f`, `DeathCueWindowDelay = 0.35f`, `DeathCueWindowDuration = 0.35f`, `DeathFallbackDuration = 4.8f`
  - `RehearsalWindupScale = 0.85f`, `TrainingChimeCharges = 3`
  - `FinaleDriftStart = 1.4f`, `FinaleDriftDuration = 2.6f`
  - `FinaleThreatDeadline = 4f`, `FinaleFinalSwipeAngle = 20f`, `FinaleOffAxisAngle = 45f`
- **Playtests:** Tier 3 adds 55 headless checks for Codex/Learned persistence and reset, legacy compatibility, training-chime limits and mastery, run-only Grace effects, death cue/window/fallback/bounded retry, bargain acceptance without bypass, rehearsal Codex recording, spatial drift, resonance-scaled threats, false prompts, decoy/off-axis damage, Finale restart at `1 HP`, and final defense/victory cleanup. The suite now runs `148/148` checks.
- **Build/test result:** Script recompile passed with no compile errors; Linux build passed with `0` errors and `974` known package/URP/shader warnings, `137,942,524` bytes. Headless suite `148/148`; headless smoke reached Victory at `3 HP` in `21.7 s` with exit code `0`. Android APK also built successfully with `0` errors and `979` known package/URP warnings, `43,337,105` bytes, debug-signed; it has not been installed because no device is connected.
- **Deviations/deferred work:** Device listening, TTS timing, haptics, and Android build verification remain pending. The Finale smoke driver uses real-time waits and is intentionally slower than the former autoplay smoke.

## Tier 3 verification

- **Status:** Passing.
- **Automated suite:** `-omtkPlaytestSuite` passed `148/148` checks, including Tier 1 and Tier 2 regressions and 55 Tier 3 checks.
- **Smoke run:** `-omtkPlaytest` completed Wave 3, skipped the interactive death rehearsal once, and reached Victory at `3 HP` in `21.7 s` with exit code `0`.
- **Build:** Linux x86_64 headless build passed, `137,942,524` bytes, `0` errors, `974` known warnings. Android ARM64 debug APK passed, `43,337,105` bytes, `0` errors, `979` known warnings. No device was connected for installation.
- **Environment:** Linux headless. Device audio, haptics, TTS, and the Android build remain unverified.

## Tier 4 — Automated Playtests, Build Verification, and Documentation

- **Status:** Implemented; Tier 4 runtime suite passing.
- **Behavior hardened:**
  - Parry cooldown is now narrator-independent at the configured `1.25 s`; standard parries still receive the `0.7x` shortening. This removes the previous `0.9 s`/`1.3 s` mode-specific behavior that contradicted the design and increased cooldowns for TTS.
  - Victory now clears the tension target immediately as well as stopping the tension source, so `TensionActive` is false as soon as the victory sequence begins.
  - Radar blips are hidden even when assist is toggled off after a frame in which blips were already hidden, avoiding stale markers.
- **Playtest coverage added:** 49 checks, for a total of `197/197`:
  - Grace boundary at `25°` and rejection just outside it; standard parry cooldown at `0.7 × 1.25 s` in TTS, SYNTH, and OFF narrator modes; perfect cooldown remains `1.25 s`.
  - Stalker closes during approach; Charger midpoint tempo is linear and rear recharge closes from the overshoot distance.
  - Conjurer decoy timing (real windup at `0.4 s`, no premature launch), dual-token offbeat scheduling at `0.55 s`, decoy sub-bass omission and retained `980 Hz` tone via spectral check.
  - Attempt-one onboarding bearings (wave 1 `150°`, wave 2 `-74°`), wave-start scheduling waits for narration in TTS/SYNTH modes, transition cleanup of attack tokens/time scale/audio filters/speech waits, radar blip and non-directional ring checks.
  - Death fatal cue pitch `0.62`, `1600 Hz` low-pass, and fatal position; misaligned and mistimed rehearsal failures; recovery after failure; aligned rehearsal bargain rejection; repeated deaths do not inflate HP or grant run power; skipped rehearsal returns control within the bounded delay; reset clears deaths and best wave.
  - Finale voice drift midpoint; explicit timeout damage; wrong final defense and wrong final swipe each cost HP and cannot win; recovery after a wrong swipe; victory clears tension and preserves Codex.
- **Files changed:** `Assets/Scripts/OMTK/OMTKGame.cs`, `Assets/Scripts/OMTK/OMTKAudio.cs`, `Assets/Scripts/OMTK/OMTKPlaytest.cs`.
- **Build/test result:** Script recompile passed; Linux build passed with `0` errors and `974` known package/URP/shader warnings, `137,115,660` bytes. Headless suite `197/197`; headless smoke reached Victory at `3 HP` in `31.01 s` with exit code `0`. Android APK rebuilt with `0` errors and `981` known warnings, `43,346,193` bytes, `131.2 s`, debug-signed; no device was connected for installation.
- **Deviations/deferred work:** Device listening, gyro/touch latency, TTS timing, haptics, clean-state device behavior, release signing, and AAB packaging remain pending. The Unity AI/Sentis package still emits unrelated shader warnings. The Finale smoke uses real-time waits and is intentionally slower than the former autoplay.

## Tier 4 verification

- **Status:** Passing.
- **Automated suite:** `-omtkPlaytestSuite` passed `197/197` checks.
- **Smoke run:** `-omtkPlaytest` completed Wave 3, skipped the interactive death rehearsal once, and reached Victory at `3 HP` in `31.01 s` with exit code `0`.
- **Linux build:** `build_31e1d5e36c33`, `0` errors, `974` known warnings, `137,115,660` bytes.
- **Android build:** `build_842af1e8ceda`, APK `43,346,193` bytes, `0` errors, `981` known warnings, `131.2 s`, `versionCode=1`, `versionName=0.1.0`, `minSdk=31`, `targetSdk=36`, ARM64, debug-signed. Not installed; no device attached.
- **Environment:** Linux headless. Device audio, haptics, TTS, gyro feel, and release packaging remain unverified.

## SVG UI art pass — obsidian & moonlight visual identity

- **Status:** Implemented; Tier 4 runtime suite passing unchanged.
- **Visual upgrades:** The code-only uGUI layer (plain text + pulse ring) is upgraded with an original, code-authored SVG art set ("obsidian & moonlight": silver hairline linework, ember-gold accents, engraved heraldry), rasterized at 2x into `Assets/Resources/UI` and loaded at runtime with procedural-sprite fallbacks so builds without textures still run.
  - Full-screen vignette with floor rings, pillar hairlines, and dust motes; rotated ornament corners hug the safe area.
  - HP is now three engraved shield pips (gold-filled / dim-outline); the wave is three diamond pips with a small `ATTEMPT n` readout; the old `HP n/3` line remains as a fallback only when textures are missing.
  - Prompt cues gained a matching glyph (tap / turn / listen / swipe / breathe / trace / wait) that pops and settles above the prompt word; the long options footer has no glyph.
  - Title screen gained an etched knight-helm sigil above and a diamond-flecked rule below; both follow the title's visibility; victory adds a breathing laurel plate that fades in.
  - New feedback layers: damage pulses a red edge vignette (reuses the existing `damageFlash` decay) and perfect parries bloom a cyan flash from the ring (reuses `parryFlash`). Resonance ring, radar dial, blip core, and glow are now higher-fidelity SVG art drawn in white so the existing state-based tinting is unchanged.
- **UX/layout hardening:** A `SafeRoot` container parents all edge HUD (status, shields, pips, prompt, subtitle, corners) and insets itself from `Screen.safeArea` at 1080x1920 reference scale, recomputed on screen/safe-area changes, so notches, rounded corners, and TV overscan no longer clip critical UI.
- **Files changed:** `Assets/Scripts/OMTK/OMTKGame.cs` (CreateUI rewritten, UpdateVisuals/UpdateHud extended, safe-area + prompt-glyph helpers), new `Assets/Scripts/OMTK/OmtkArt.cs` (cachedResources loader), new `Assets/Editor/OmtkArtImport.cs` (import settings), new `Docs/UI/svg/*.svg` (21 sources), `Docs/UI/render.sh` (rsvg-convert 2x pipeline), `Docs/UI/preview/` + `Docs/UI/preview.html` (layout mock), 21 rendered PNGs in `Assets/Resources/UI/`.
- **Build/test result:** Script recompile passed with `0` errors; Linux build passed, `146,833,788` bytes. Headless suite `197/197` with exit code `0`; headless smoke reached Victory at `3 HP` with exit code `0`. Android APK rebuilt with `0` errors, debug-signed, `44,450,183` bytes (`+1.1 MB` from the texture pack); no device was connected for installation.
- **Deviations/deferred work:** On-device visual pass (notch insets, DPI sharpness, gradient banding under ETC2) remains pending per the existing device-verification gap; the PNG rasterization step requires `rsvg-convert` when the art set changes.

## Run pause and directional hit feedback

- **Status:** Implemented; device-verified on `ZA222JRC3B`.
- **Run pause:** Holding a stationary finger for `0.5 s` during Intro, Playing, Interlude, or Finale pauses the run. Pausing freezes the state machine and enemy ticks, silences all engine audio via `AudioListener.pause` (windups resume exactly where they were), stops narration, and shows a dimming plate with `PAUSED`, a rule, and the prompt `TAP TO RESUME · SWIPE DOWN TO ABANDON` while the narrator speaks the options (system TTS is unaffected by the listener pause). Tap resumes after a `0.45 s` guard lockout; swipe down and abandon the run to the title cleanly (bargains cleared, deaths unchanged, music/stops/tension reset). Calibration and Death are excluded on purpose (bounded scripted states). `P`/`Escape` mirror pause/resume on desktop. App lifecycle pause no longer un-mutes a run pause (`AudioListener.pause = paused || runPaused`).
- **Directional hit feedback:** The full-screen red edge vignette was removed as too broad. Getting hit now flares a short red arc on the resonance ring at the attacker's bearing, snapshotted relative to the player at hit time: `1.0` strength on damage, `0.75` through the Iron bargain, `1.5` on the fatal blow, and finale penalties flare toward the Conductor bearing. Each flare fades over roughly `0.4-0.6 s` with a slight outward scale. New `hit_arc` SVG joins the art set.
- **Files changed:** `Assets/Scripts/OMTK/OMTKGame.cs` (pause state/lifecycle, hit arc, input branches, app-pause interplay), `Docs/UI/svg/hit_arc.svg` + rendered texture, `README.md`, this log.
- **Build/test result:** Script recompile passed with `0` errors; headless suite `197/197` (pause is gesture-gated and cannot affect the reflection-driven suite); headless smoke reached Victory at `3 HP` with exit code `0`. Android APK rebuilt with `0` errors, debug-signed, `44,465,261` bytes. On-device (`ZA222JRC3B`): installed, launched, started a run, paused via hold, resumed via tap, re-paused and abandoned via swipe-down back to title; `Home`/`echo` captures decoded cleanly and logcat shows no errors. Evidence captures: `Docs/screenshots/device-svg-title.png`, `device-svg-paused.png`, `device-svg-title-after-abandon.png`.
- **Deviations/deferred work:** No pause state inside Death rehearsal (bounded timing); pause has no in-screen restart option (abandon covers it); audio resume of long windup loops relies on `AudioListener.pause` semantics, which should be spot-checked on the device with headphones.
