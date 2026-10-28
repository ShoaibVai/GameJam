# One More Time, Knight

### Implemented game design — written against the shipped code

**Engine:** Unity `6000.6.7f1`, URP, single scene. The camera, UI, audio, enemies, and state machine are all constructed at runtime (`Assets/Scripts/OMTK/`).
**Platform:** Android 12+ (`minSdk=31`, `targetSdk=36`), ARM64, IL2CPP, portrait. Linux/Windows dev builds verified headless.
**Input:** New Input System only — gyroscope yaw, touchscreen, keyboard/mouse fallbacks.
**Audio:** 100% procedurally synthesized at runtime (`AudioSynth.cs`). No imported music, voice, or SFX assets.

---

## 1. Acoustic-First Design Thesis

A blind knight fights by ear alone. Every mechanic is an acoustic skill:

- Threat **direction** = spatialization (mono 3D panning + rear low-pass + distance rolloff).
- Threat **timing** = the windup tell; its character and tempo (e.g. the Charger's accelerating pulse).
- Threat **attention** = mix attenuation: the enemy nearest your facing stays bright, others are muffled.
- Threat **identity** = spectral separation (each variant has a distinct presence loop and windup; the Conjurer's decoy deliberately omits sub-bass).
- **Knowledge is the only permanent progression**: learn the tells, never gain stats.

The screen shows a mood ring, hearts, and text — never a bearing. (Radar assist is an explicit opt-in accessibility layer, default off.) Holding the phone flat and not turning cannot win: enemies out-range the radar-free ring, and perfect parries require physically orienting toward the tell.

---

## 2. Core Loop

1. **Title** — spoken controls; hold (0.75 s) to open accessibility options.
2. **Intro** — run start. On Attempt 1 only, a short **calibration** precedes Wave 1.
3. **Calibration** — a tone sounds at `150°` from `3.2 m`. Turn until aligned within `15°` and tap to confirm; misaligned taps are refused with guidance; the run auto-continues after `10.5 s`. Radar is suppressed during calibration.
4. **Waves 1–3** — escalating enemy mixes (see §5). Interludes heal +1 heart (max 3).
5. **Death** — the fatal blow is replayed, slowed and low-passed at its original position, for an interactive rehearsal plus a bargain offer.
6. **Retry** — new attempt; after Attempt 1 the Conductor "edits" the encounter (see §5).
7. **Finale** — three beats against the Conductor itself (§8).
8. **Victory** — silence, sheathe sound, run-scoped bargains cleared, Codex preserved.

**Hearts:** fixed at 3. An attack costs 1 (Iron absorbs one hit per bargain). Interludes heal 1. Loss of the last heart enters the Death state.

---

## 3. Controls and Accessibility

| Input | Action |
|---|---|
| Turn phone/head (gyro yaw); A/D or Right/Left arrows | Aim listening direction; recentered at run start, wave start, and retry |
| Horizontal drag | Screen-based yaw fallback (`0.18°/px`); disabled while a staggered enemy is slashable; swipes suppress turning |
| Tap (`<45 px`, `<0.42 s`; `Space`) | Parry, calibration confirm, death rehearsal tap, Finale defense tap, start run, next wave, restart after victory |
| Horizontal swipe (`>90 px` or `>800 px/s`, `<0.48 s`; `F`) | Slash a staggered enemy; start a run from the title/options next row; strike in the Finale |
| Swipe up (death only; `Up arrow`) | Accept the offered bargain (the aligned rehearsal tap is still required before retry) |
| Hold ≥ `0.75 s` on title | Toggle accessibility options (`O` opens; `Esc`/two-finger closes) |
| Two-finger tap | Skip death rehearsal; close options |
| Two two-finger taps quickly | Recenter yaw and speak heading + HP (`R` recenters; `H` repeats last cue + heading) |

**Options rows (tap changes, swipe moves to next, hold/two-finger closes):**
1. **Narrator:** SYSTEM TTS (default) → SYNTH → OFF. Without a system TTS voice, the stylized SYNTH voice is the fallback; subtitles always carry exact wording.
2. **Haptics:** pulses on damage, perfect parries, kills, bargains, death (default on).
3. **Radar assist:** off by default, persisted; opt-in only.
4. **Volume:** cycles `100% → 75% → 50% → 25% → 0% → 100%`, persisted.
5. **Reset progress:** clears deaths, best wave, Codex, mastery, and run state; accessibility settings persist.

Settings persist in `PlayerPrefs` (`OMTK_NarratorMode`, `OMTK_Haptics`, `OMTK_RadarAssist`, `OMTK_Volume`).

---

## 4. Combat Rules (exact, from `OMTKConfig.cs` and `OMTKGame.cs`)

### Resonance lock
Face an enemy within `35°` for `0.3 s` continuously to lock it. Leaving the cone drops the lock at once. **Perfect parries require the lock.** Locked enemies play a distinct spatial lock sound.

### Parry (tap)
- **Premature tap:** valid target with `>0.35 s` windup remaining, or any tap while live enemies exist but none is winding → **guard lockout** `0.45 s` + **Tinnitus** (`900 Hz` low-pass, `0.8 s`). An interrupted offbeat token is cancelled.
- **Perfect:** within `18°` (Grace `25°`), windup remaining `≤0.14 s` (Grace `0.22 s`), resonance-locked → **stagger `1.2 s`**, prompt "Now. Swipe." If another token is winding, the **time dip** triggers (`Time.timeScale = 0.65` for `0.4 s` unscaled, then restored; cancelled by any state change).
- **Standard:** within `35°`, windup remaining `≤0.35 s` → blocks the attack without a stagger; the enemy recovers `0.42 s` and retreats to `6.6 m`.
- **Glancing:** `35–40°` → deflect, recovery `0.34 s`, retreat to `7.6 m`. For a **Charger**, a glance instead overshoots to `4.5 m`, reverses its bearing `180°`, and re-charges from behind at `1.15×` speed.
- **Off-axis:** `>40°` cannot parry — whiff + full guard-lockout penalty.

### Attack cooldown (narrator-independent, verified in all three narrator modes)
- Base `1.25 s` after any parry. **Standard parries get `0.7×` → `0.875 s`.** Perfect and glancing take the full `1.25 s`.
- Successful slash: `0.82 s`. Shove: `0.95 s`. Wave start: `1.8 s`. The Attempt-1 scripted tutorial death suppresses attacks until the forced kill.

### Slash (horizontal swipe)
- Requires a **staggered** enemy within `35°` of your facing → kill. Off-axis swipes cannot bypass this check.
- Tapping a staggered enemy **shoves** it to `7.4 m` with `0.5 s` recovery — the stagger is lost ("Swipe to finish").
- Swiping with nothing staggered whiffs.

### Windup modifiers
- Base windups: Stalker `0.85 s`, Charger `0.65 s`, Conjurer `0.95 s`.
- Rehearsal bonus (next attempt, same variant, per rehearsal success): `×0.85`.
- Stillness bargain: `×1.12`.
- Approach speed scales `+7%` per attempt (cap `+30%`); the Hasten edit adds `+18%` more.

---

## 5. Enemies (three archetypes) and Scheduling

Shared spawn: `9.2 m + 1.1 m per enemy index` away; approach target `2.35 m`; attacks launch at `≤3.05 m`. Each enemy has a 3D `AudioSource` plus its own low-pass; front/back is conveyed by a `720 Hz`–`5000 Hz` cutoff curve (bright in front, dull behind).

### The Stalker
Low sub-bass presence. Approaches at `1.25 m/s` while its bearing oscillates `±32°` (`0.55 Hz` sine); the bearing **locks** the moment the windup starts. Long `0.85 s` windup teaches: track the drifting source, then commit.

### The Charger
Rapid accelerating tick. Approaches fastest (`2.35 m/s`); short `0.65 s` windup. **Deterministic distance-to-tempo mapping:** pulse rate scales linearly from `1 Hz` at `10 m` to `5.5 Hz` at `2.35 m` (midpoint `2 m` ≈ `3.25 Hz`, test-verified). Its glancing consequence sends it behind you for a rear re-charge (see §4).

### The Conjurer
Two-note phantom shimmer. Approaches slowly (`0.95 m/s`) with a `±13°` drift, and **always plays a phantom decoy loop at `±90°`** from its true bearing (side alternates by spawn index). When it reaches strike range it plays a **decoy windup** at the decoy bearing; the **real windup** follows `0.4 s` later at the true bearing. The decoy omits sub-bass energy (spectral test: `<20%` of the real windup's sub-bass magnitude) while keeping the mid/high tone. Tapping the decoy bearing is an off-axis whiff → lockout + Tinnitus.

### Dual-token scheduling
- Only one **primary token** and at most one **offbeat token** may wind up at a time (`primaryToken`/`offbeatToken`).
- At Wave 3 or Attempt 3+, when the primary launches, an offbeat token is queued at `+0.55 s` (minimum enemy separation `85°`).
- A perfect parry of the primary while the offbeat is winding triggers the `0.65×` time dip for `0.4 s`.
- The scheduler waits for wave narration (TTS/SYNTH) to finish before launching Wave 1 enemies.
- Wave 3 **attention system:** the enemy nearest your facing gets attention `1.0`; all others drop to `0.35` with their low-pass cutoff scaled by `0.45` — you must physically turn between voices to keep cues legible.

### Waves and encounter variation
| Wave | Attempt 1 (fixed) | Attempt 2+ | Attempt 3+ / edits |
|---|---|---|---|
| 1 | 1 Stalker at `150°` | 1 Stalker near `62° ± 32°` | — |
| 2 | 1 Charger at `-74°` | 1 Charger near `-74° ± 40°` | +1 Stalker opposite |
| 3 | Conjurer at `142°` + Stalker at `-38°` | Conjurer near `142° ± 34°` + separated Stalker (≥`85°`) | +1 Stalker at the most open bearing (or via Add Voice) |

After Attempt 1, each retry rolls a **Conductor edit**: `SwapCast` (Charger↔Stalker), `Hasten`, or `AddVoice` — narrated as a hint on the retry screen.

---

## 6. Radar, Ring, and Calibration

- **Radar assist** is **off by default** and toggled in the title options; the preference persists.
- When on, in Playing/Finale/Death states it shows a dial, cardinal labels, a `HDG 000°` readout, and blips: enemy blips (color-coded per variant, bright while winding, hollow while staggered, shown at true azimuth/distance), a Conductor true-bearing blip in the Finale, and the fatal-azimuth blip during death replay. It also plays periodic radar **pings** (interval `1.35–2.1 s`, closer threats = tighter interval).
- Hidden entirely during Calibration and whenever assist is off; turning it off also clears stale blips within one frame.
- **The resonance ring never exposes a bearing**: it is rotation-locked, centered, and only pulses with the mix RMS plus parry/damage flash tints. The test suite asserts zero rotation while assist is off.

---

## 7. Death, Rehearsal, Bargains, and Progression

### Death sequence (deterministic, bounded)
1. The killer's azimuth, windup clip, and world position are captured; all enemies are cleared.
2. A low death note lands at `1.05 s`.
3. **Fatal-cue replay:** the killer's windup replays at its original position, at `0.62×` pitch through a `1600 Hz` low-pass. First replay at `2.1 s`, repeating every `1.8 s`.
4. Each replay opens a **tap window** `0.35 s` later and `0.35 s` long. Align within `25°` of the fatal bearing and tap inside the window to succeed.
5. **Success:** the variant is permanently recorded in the Codex; the next attempt's same-variant windups run `×0.85`; retry after `0.55 s`.
6. **Failure/timeout:** misaligned or mistimed taps give spoken feedback and the cycle repeats until `4.8 s`, when "The echo fades" resolves automatically. `deathRetryAt` is capped so control returns within **5 s** in all paths (fallback, success, or skip).
7. **Skip:** `Esc`/two-finger tap immediately resolves without granting the bonus.
8. Attempt 1's scripted first death is deterministic and **cannot be parried** ("This blow cannot be stopped.") — it guarantees the rehearsal tutorial.

### Bargains (run-scoped)
- Each death offers exactly one: the earliest unaccepted Echo in fixed order Foresight → Grace → Iron → Stillness.
- **Accept** with a swipe up (or a qualifying double-tap) any time before a successful rehearsal tap. An aligned rehearsal tap **rejects** the open offer ("The echo waits. Tap to answer it.").
- Effects: **Foresight** — an early `1.3×`-pitch chime marks the perfect window; **Grace** — `0.22 s`/`25°` perfect window; **Stillness** — `×1.12` windups; **Iron** — absorbs one hit (RunEchoMask bit cleared on use).
- Accepted bargains persist across retries within a run (retry does not clear them). Each acceptance also raises `ConductorResonance`, which grows the finale harvest. A new run (`StartRun`) and Victory clear all run bargains and resonance.

### Codex, mastery, training chimes (persistent)
- **Codex discovery** happens on a successful death-rehearsal tap, not on kills.
- **Training chime:** for Codex-known, not-yet-mastered variants, windup cues get an extra chime; up to **3 charges per attempt**, recharged on run start and retry.
- **Mastery:** a perfect parry **without** the training chime permanently masters the variant; the chime stops for that variant forever.
- Persists across retries and app restarts via `OMTK_CodexMask`/`OMTK_LearnedMask`. No HP, damage, speed, or window growth ever persists.
- `OMTK_Deaths`, `OMTK_BestWave` persist. **Reset progress** (options row 5) clears deaths, best wave, Codex, mastery, and run state; accessibility settings remain.

---

## 8. The Conductor Finale

Tension layer on; music ducked; three beats.

### Beat 1 — Drift
At `1.4 s` the Conductor speaks ("Do you hear me now?") from `0°` at `4 m` and drifts to `180°` over `2.6 s`. Pure ear-tracking (a blip exists only with radar assist).

### Beat 2 — Harvest
- **Threat count = `1 + ConductorResonance`** (1–5 depending on bargains taken this run).
- Each threat: true bearing starts at `180°` and shifts `±90°` (direction alternates) after every resolve; the decoy mask sits at true `±90°` (alternating per threat).
- Timeline per threat: spatial whisper at `0.9 s` — the **true** Conductor sound at `0.95` volume on the true bearing, a **decoy** windup at `0.45` volume on the decoy bearing; a **tap window** opens `1.25 s` in for `0.35 s`; hard deadline `4.0 s`.
- **Success:** aligned tap (`≤40°`) inside the window. Threat consumed; next after `0.75 s`.
- **Failures (each −1 HP):** tap outside the window, off-axis tap (`>40°`), **any swipe during a live threat** — swiping `>45°` off the true bearing is "The blade finds only air", or swiping near the decoy bearing (`≤35°`) is "You struck a decoy." — or timeout.
- Deceptive text prompts ("STRIKE FRONT", "TURN LEFT", "BEHIND YOU") are **text-only trolls**; they never move the true bearing (asserted by test).

### Beat 3 — Climax
- Final windup cue at `1.0 s`; **defense window** `1.25–1.60 s`; deadline `4.0 s`.
- Defend with an aligned tap (`≤40°`) inside the window. Once defended: "Now. Swipe." — a horizontal swipe within `20°` of the true bearing wins.
- Wrong-time tap, off-axis tap, wrong-line swipe (`>20°`), or timeout each cost 1 HP and reset the climax cue with a `0.8 s` shortened lead.

### Failure and victory
- HP reaching 0 inside the finale restarts **only the Finale** at `1 HP`, back at Beat 1: "Not yet. Again." (Iron absorbs one finale hit.)
- **True Victory** requires the final defense plus the correctly aimed swipe: `finaleEnding` silences the tension and Conductor immediately, `1.5 s` of silence, a sheathe sound, then Victory state — run bargains and resonance cleared, permanent Codex preserved, best wave set to 3.

---

## 9. Audio Language (implemented)

- All cues are synthesized in `AudioSynth.cs` and scheduled by `OMTKAudio.cs`: variant windups, presence loops, decoy approach/windup, parry/perfect/glance/whiff, shove, slash, stagger, lock, kill, tinnitus bed, heartbeat, music/tension layers, narrator blips, radar pings, calibration tone, training/Foresight chimes, death note + fatal replay, sheathe, victory.
- **Spatialization:** `spatialBlend = 1`, distance rolloff `1.2–12 m`; per-source `AudioLowPassFilter` front/back: enemies `720→5000 Hz` (quadratic), attention-scaled (`×0.45` for unattended), phantom `1100–5000 Hz` by attention, death replay fixed `1600 Hz`.
- **Tinnitus:** `900 Hz` low-pass bus-live filter during the `0.8 s` premature-tap penalty; restored (to open music) after `0.8 s` or any state transition (asserted by tests).
- **Narrator:** SYSTEM (Android TTS), SYNTH (stylized), OFF (subtitles only). Narration ducks music; wave-start scheduling waits for speech to complete.
- **Subtitles:** high-contrast yellow text for every spoken line, on always.

---

## 10. Testing, Builds, and Known Gaps

- **Automated suite** (`Assets/Scripts/OMTK/OMTKPlaytest.cs`): `197/197` headless checks — deterministic bearings/timings, no reliance on random favorable outcomes. Covers all §4 combat boundaries, cooldown modes, resonance, shove/slash, decoy spectra (DFT), dual-token/timing, attention, death rehearsal, bargains, persistence/reset, radar non-directionality, calibration, transitions, and the full Finale failure/victory matrix.
- **Headless smoke:** Linux x64 `-omtkPlaytest` reaches Victory at 3 HP in ~`31 s` (exit 0).
- **Builds:** Linux verified; Android ARM64 APK (debug-signed) builds with `0` errors and only third-party package/URP/Sentis shader warnings (no OMTK warnings).
- **Unverified on hardware:** phone audio listening, gyro feel, TTS timing/voices, haptics, touch latency, frame rate. Requires an attached device pass.

---

## 11. Deliberate Divergences from the Pre-Jam Concept

- **No imported asset packs.** The concept doc listed commercial packs; every sound is procedural instead (deterministic, tiny build, no licensing).
- **"Flanker" became the Conjurer** (phantom/decoy specialist) — strafing alone added less acoustic-deception value than a spectral decoy.
- **Wave sizes are 1/1(+1)/2(+1) with scheduling**, not 3/4/5 simultaneous spawns — the dual-token scheduler keeps the spatial mix legible with at most two concurrent windups.
- **Radar assist exists as an opt-in accessibility layer** (default off, persisted, hidden during calibration). The pre-jam doc said "no radar" — the consideration is the resonance ring still gives no bearing.
- **Cut:** Resonance Surge combo, invulnerability frames, pity "Second Wind", title sword art. Combos conflicted with "one committed attack per tell"; invulnerability blurred the damage rules; Second Wind is replaced by the deterministic tutorial death + rehearsal teaching path.
- **Perfect window is `0.14 s`/`18°`** (the concept's `0.15 s`/`20°` were tightened), and parries are gated by a **resonance lock** the concept didn't have — clearing makes perfect parries an act of confident aiming, not reaction.
- **Bargains unlock by acceptance order** on every death, not by a death-count table — simpler, narratively coherent, and provides a deterministic finale-harvest size.
- **Death rehearsal duration is bounded at `4.8 s`** with retry within `5 s` total — the concept's "bounded delay" is an exact spec now.
