# OMTK Redesign Change Log

This log records implementation work against the acoustic redesign specification. Build results are reported only for checks that were actually executed.

## Tier 1.1 — Directional Parry Grades and Tinnitus

- **Status:** Implemented; playtest verification pending.
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
- **Playtests:** The harness now faces the token before tapping and faces staggered enemies before swiping. Full boundary coverage remains scheduled for Tier 4.
- **Build/test result:** Compile check passed with exit code 0 and `Tundra build success`; no compiler errors or warnings were reported. Playtests were not executed.
- **Deviations/deferred work:** The required automated perfect/standard/glancing boundary tests are deferred to Tier 4.

## Tier 1.2 — Separate Tap Parry from Swipe Finisher

- **Status:** Implemented; playtest verification pending.
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
- **Playtests:** Harness yaw handling updated. Dedicated tap-versus-staggered and off-axis swipe tests remain scheduled for Tier 4.
- **Build/test result:** Compile check passed with exit code 0 and `Tundra build success`; no compiler errors or warnings were reported. Playtests were not executed.
- **Deviations/deferred work:** Automated gesture-isolation tests are deferred to Tier 4.

## Tier 1.3 — Resonance Lock and Lock-In Audio

- **Status:** Implemented; playtest verification pending.
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
- **Playtests:** Not yet added; lock acquisition and perfect-parry enforcement are scheduled for Tier 4.
- **Build/test result:** Compile check passed with exit code 0 and `Tundra build success`; no compiler errors or warnings were reported. Playtests were not executed.
- **Deviations/deferred work:** Lock behavior has not yet been verified on a device or in play mode.
