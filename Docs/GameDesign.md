# One More Time, Knight
### A 48-hour GameJamPlus design for the theme “One More Time”

**Working title:** *One More Time, Knight*  
**Original concept:** *The Last Knight of the Resonant Order*  
**Genre:** Audio-first action roguelite  
**Platform:** Android 12+, portrait, one-handed, headphones required  
**Session:** 60–90 second runs; 5–10 minute first loop; replayable  
**Team:** Solo  
**Engine:** Unity 6000.6.7f1, URP, one scene

---

## 1. High Concept

A blind knight is resurrected by a dying order every time he falls. He fights by sound in a dark arena. Each death isolates the sound that killed him, teaches him the tell, and grants a new “Echo” before the next attempt. The voice that resurrects him is the final enemy.

The player keeps pressing “one more time” because every attempt is short, legible, and slightly different — and because the voice keeps changing.

---

## 2. Player Fantasy

“I am a blind knight who sees the world through sound. I can hear the shape of the monster before it strikes. Every death makes me better.”

The phone is the hilt. The swipe is the blade. Turning your body turns your ears. The world is invisible, but it is not silent.

---

## 3. Theme: One More Time

The theme is not just the narrator’s line. It is the structure:

- **Short runs:** 60–90 seconds. Low commitment.
- **Instant retry:** under one second. Death is a lesson, not a loading screen.
- **Persistent knowledge:** the Echo Codex remembers every tell you learn.
- **Changing attempts:** narrator lines, music layers, enemy mixes, and Echo blessings change each run.
- **Near-miss tension:** most deaths happen on the last enemy of a wave.
- **Thematic payoff:** the voice saying “one more time” is the reason the loop exists — and the final enemy.

The player should say “one more time” before the game does.

---

## 4. Core Loop

1. **Listen.** Spawn in a dark arena with 3 hearts. Enemies approach from eight directions.
2. **Turn.** Rotate the phone/body until the sound is centered.
3. **Read.** Each enemy has a distinct approach loop and a wind-up tell.
4. **Strike.** Swipe during the wind-up window to parry or kill.
5. **Learn.** If you die, the killing sound is isolated and replayed. The narrator says “One more time.” An Echo is granted. Retry instantly.
6. **Escalate.** Clear three waves to face the Conductor — the voice itself. Turn and strike it to break the loop.

---

## 5. Controls

| Action | Input |
|---|---|
| Turn / aim | Gyro yaw; recenter on retry; touch-drag fallback |
| Strike | Swipe up/forward anywhere; 0.2s cooldown |
| Perfect parry | Strike in the last 0.15s of the wind-up |
| Retry | Tap anywhere |
| Echo Pulse (optional) | Two-finger tap; stereo ping sequence |

No back-tap. No physical swing. No voice commands. These are post-jam features.

**Headphones required.** Wired headphones are recommended for lowest latency. The phone speaker cannot deliver reliable front/back cues.

---

## 6. Combat Rules

- Enemy states: **Spawn → Approach → Wind-up → Strike → Recover → Dead.**
- Wind-up: 0.9s Stalker, 0.7s Charger, 0.8s Flanker.
- Hit window: facing within ±35°, swipe during wind-up.
- Perfect parry: facing within ±20°, last 0.15s. Stuns all enemies for 1s and restores 1 heart (max 3).
- Miss: lose 1 heart, 1s invulnerability, audio duck, haptic thud.
- Combo: five kills without damage triggers **Resonance Surge** — the next strike is a 360° shockwave. Music swells.
- Wave sizes: 3, 4, 5 enemies. Spawn interval tightens from 2.5s to 1.8s.

**Fairness rule:** every attack has an audible tell, and every tell is directional. No off-screen attack without a cue.

---

## 7. Enemies

One base enemy behavior tree, three configurations. This is the jam scope.

### The Stalker
- Low sub-bass drone.
- Slow approach, long wind-up, attacks from behind.
- Teaches turning and the behind-low-pass rule.

### The Charger
- Accelerating tick.
- Fast dash, short wind-up.
- Teaches timing and the perfect parry.

### The Flanker
- Two-note call.
- Strafes sideways, attacks from the side.
- Teaches tracking a moving source.

### The Conductor
- The narrator’s voice. No body.
- After wave 3, it says “One more time” and becomes spatial behind the player.
- The player must turn and strike it within 3 seconds. One hit ends the loop.

All monster audio is built from the **Zombie Sound Pack**, pitched down and layered with **Hit The Deck** thuds. The Conductor uses processed narrator VO and **Wind Chimes**.

---

## 8. Echoes: The Retry Blessing

After each death, the Order grants one Echo automatically. The narrator announces it. No choice screen in the jam build — this keeps retry instant and avoids UI complexity.

| Echo | Effect |
|---|---|
| **Keen Ear** | +15° hit angle |
| **Slow Blood** | +15% enemy wind-up |
| **Second Wind** | +1 max heart |
| **Hush** | One free hit this run |
| **Echo Blade** | Perfect parries send a shockwave |

Jam build: implement **Keen Ear**, **Slow Blood**, and **Second Wind**. The others are stretch goals.

Echoes last for one run. The only permanent progression is the **Echo Codex** — a list of tells you have learned. Knowledge survives the loop; power does not.

---

## 9. Death and Retry Flow

1. Player loses last heart.
2. Time slows; all audio muffles except the killing sound.
3. The killing tell is replayed in isolation with its direction.
4. Narrator: “One more time.”
5. Echo is granted and announced.
6. Screen fades; player taps; new run starts.

Target: **under 4 seconds from death to control.** The death recap is the tutorial. It must be short, clear, and replayable.

---

## 10. Difficulty and Pacing

- **Attempt 1:** Stalkers only; 3 enemies; tutorial.
- **Attempt 2:** Stalkers + Charger; 4 enemies.
- **Attempt 3:** All three variants; 5 enemies.
- **Attempt 4+:** Mixed waves; +1 enemy every two attempts; music layers.
- **Pity system:** after three consecutive deaths at wave 1, grant Second Wind automatically.
- **Target:** a new player finishes in 3–6 attempts; an experienced player in 1–2.

The game should make death feel like progress, not punishment. If a death feels unfair, fix the tell, not the damage.

---

## 11. Audio Design

### Spatial language
- Phone yaw = listener yaw.
- Mono 3D sources; no HRTF.
- Left/right: panning.
- Front/back: behind-the-head low-pass (800–5000 Hz) plus a subtle volume dip.
- Distance: linear attenuation.
- Occlusion: none; the arena is open.

**Tutorial rule:** “If the sound is dull, it is behind you. Turn until it shines.”

### Enemy tells
- Stalker: drone → inhale → strike.
- Charger: tick → accelerated tick → dash.
- Flanker: two-note call → held second note → slash.
- Conductor: narrator line → “One more time” → spatial laugh.

### Player feedback
- Swipe: bright whoosh.
- Hit: chime + thud.
- Perfect parry: bell + 0.2s time-slow.
- Damage: low thud + heartbeat.
- Low health: heartbeat layer.
- Resonance Surge: rising choral swell.

### Music
- **Dark Atmospheric FREE TRACK Music Pack** with adaptive layers.
- Add one layer per attempt; add a combat layer per wave.
- Duck music under VO; master limiter; no clipping.

### Narrator
- Recorded voice, close-mic, slight reverb, pitched down slightly.
- 2D “in-head” for most of the game.
- Becomes 3D behind the player at the final twist.
- 8–10 lines total. Subtitles on by default.

### Owned assets
- Zombie Sound Pack — monsters.
- Demo Ancient Magic Pack — sword/parry/impacts.
- SoundBits Free Sound FX — whooshes, impacts, UI.
- FREE Casual Game SFX Pack — UI, timer, earcons.
- Footsteps - Essentials — mono footsteps.
- Wind Chimes — resonance bell, safe cue, Conductor.
- Dark Atmospheric FREE TRACK Music Pack — adaptive score.
- Hit The Deck — body impacts and parries.

**Do not use** Meta XR Audio SDK (Quest-only) or FMOD (build risk) in the jam build.

---

## 12. Visual Design

- Black screen.
- A thin, symmetrical resonance ring pulses with the mix. It never shows enemy direction.
- HUD: hearts, wave, attempt. Large, high contrast, minimal.
- Title: “One More Time, Knight.” Tap to listen.
- Death: “One more time.” Tap to retry.
- Win: “The loop breaks.”
- Optional title-screen sword silhouette from **Free magic swords** or **Stylized Fantasy Armory**.
- No 3D world. No enemy indicators. No radar.

**Frame it as audio-first and blind-playable, not “audio-only.”** The ring is mood, not information.

---

## 13. Narrative

The Order is dying. The knight is its last weapon. Each resurrection costs a memory. The voice says “one more time” because it needs him.

The twist: the voice is the Conductor — the thing that killed the Order. It keeps the knight in a loop to feed on his resonance. The player breaks the loop by trusting their ears and striking the voice behind them.

### Narrator lines
- “Rise, knight. The Order has need of you.”
- “The dark has a shape. Turn until it is before you.”
- “Good. The resonance answers.”
- “One more time.”
- “You are learning. That is enough.”
- “Why do you keep returning?”
- “There is nothing left to save.”
- “I am the Order. I am the voice that keeps you.”
- “One more time.” *(spatial, behind)*
- “You are free.”

---

## 14. Technical Design

- One scene; state machine: Title → Run → Wave → Death → Retry → Boss → Win.
- `GyroAim`: `Input.gyro.attitude` yaw only; recenter on retry; touch-drag fallback. Active Input Handling = Both.
- `AudioDirector`: music layers, VO ducking, mixer groups.
- `Enemy`: state machine; configurable wind-up, speed, variant.
- `SpatialCue`: 3D AudioSource + behind low-pass.
- `RunStats`: hearts, wave, attempts, kills, combo.
- `EchoSystem`: three simple modifiers.
- `Narrator`: queue, subtitles, 2D/3D switch.
- Data: ScriptableObjects for enemy configs, Echoes, narrator lines.
- Build: Android, minSdk 31, targetSdk 36, IL2CPP, ARM64, portrait.
- Performance: 60 FPS cap; object pooling; no GC in Update; ~20 audio voices.

**Pre-jam blocker:** Android Build Support is not installed. Install `android`, `android-sdk-ndk-tools`, and `android-open-jdk` before the jam, then make a blank build on the phone.

---

## 15. Jam Scope

**Must**
- One enemy base, three variants; three waves.
- Turn, swipe, parry, hearts, instant retry.
- Three Echoes; Echo Codex line on death.
- Narrator twist; Conductor final QTE.
- Built-in 3D audio + behind low-pass; music layers.
- Black screen + resonance ring + subtitles.
- Android build tested on the phone.

**Should**
- Haptics on wind-up and damage.
- Perfect parry time-slow.
- Resonance Surge.
- Volume/recenter menu.
- Title-screen sword silhouette.

**Won’t**
- 9-pose calibration.
- HRTF / Resonance Audio.
- Full TalkBack.
- Voice commands.
- Five monster types.
- Boss phases.
- 3D world or story campaign.
- Online, procedural dungeons, multiple weapons.

**If behind schedule:** cut Echoes to one, variants to two, and the title sword. Never cut the Android build, instant retry, audio tells, or the narrator twist.

---

## 16. 48-Hour Schedule

See `Docs/JamPrototypePlan.md` for the full schedule.

Critical order:
1. Android build proof.
2. Gyro yaw + one spatial sound.
3. Swipe + one enemy + hearts.
4. Death recap + instant retry.
5. Three waves + variants.
6. Narrator + final twist.
7. Polish, device testing, submission.

Submit the build Sunday by 23:59. Use Monday–Tuesday for the pitch and trailer.

---

## 17. Pitch Hooks

- “A blind knight. A voice that says one more time. Every death teaches you the sound that killed you.”
- “60-second runs. Instant retry. A narrator you learn to distrust.”
- “Built for blind and sighted players. No graphics required. Headphones essential.”
- “The theme is the mechanic: the game is designed around the moment after death.”
- “One more time — until you learn why the voice wants you to come back.”

### Business angle
- Low art cost; audio-first production.
- Reusable enemy/audio systems; scalable waves.
- Accessible mobile niche with broad streamer appeal.
- Premium or ad-free mobile release; expansions via new monsters, Echoes, and dungeons.

---

## 18. Success Metrics

- New players finish in 3–6 attempts.
- 80% of playtesters choose “one more time” after death without prompting.
- Players can locate a sound within 30° after one minute.
- No player is confused about controls after the tutorial.
- Stable 60 FPS on the target phone.
- Build submitted on time; pitch submitted by Tuesday.

---

## 19. Open Questions

- Is the final QTE too abrupt? Playtest the last 10 seconds.
- Is the behind-low-pass clear enough on phone speakers? Headphones only.
- Does the Echo grant feel rewarding without a choice screen? Test.
- Is the narrator twist too “Something’s Off”? Keep it light; the theme is One More Time.
- Can a blind player complete the loop with only audio? Test with eyes closed.
