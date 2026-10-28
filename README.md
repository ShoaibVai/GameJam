# One More Time, Knight

A short, replayable audio-first roguelite for Android. You are a blind knight in a dark hall. Turn your head toward the sound, parry the tell, and answer with your blade. Every death teaches you the blow that killed you.

Built for GameJamPlus 2026, theme: **One More Time**.

## Play

- Android 12 or newer, ARM64 phone.
- Wired or wireless headphones are strongly recommended.
- Install `Builds/Android/OneMoreTimeKnight.apk` with `adb install -r Builds/Android/OneMoreTimeKnight.apk`.
- Launch **One More Time, Knight**, keep the phone in portrait, and tap once to begin.

### Controls

| Input | Action |
| --- | --- |
| Turn the phone / your head | Aim your listening direction; small horizontal drags are a fallback |
| Tap | Parry the incoming tell, confirm calibration, rehearse the fatal blow, or hold the final defense |
| Swipe horizontally | Slash a staggered enemy, start a run, or strike in the finale |
| Swipe up during death | Accept a bargain; still align and tap to finish the rehearsal |
| Hold on the title | Open accessibility options; tap changes the row, swipe moves to the next row |
| Hold during a run | Pause; tap to resume, swipe down to abandon the run |
| Two-finger tap during death | Skip the death rehearsal |
| Two-finger tap in options | Close options |
| Two two-finger taps | Recenter yaw and report heading and HP |
| Tap on title / victory | Start, or begin again after victory |
| Escape / two-finger tap during death | Skip the death rehearsal |
| R / H (keyboard) | Recenter / repeat the last cue and heading |

The first run starts with a short calibration: turn until the tone is centered, then tap. The Conductor speaks from behind you at the end. Tap to hold the final defense, then swipe toward the true voice. Death rehearsal has a 4.8-second replay limit; retry follows automatically within 5 seconds.

### Accessibility

The title and all narration are spoken through Android's system text-to-speech by default. If the system voice is unavailable, the game falls back to its synthesized voice.

- **Narrator:** System TTS (default), synthesized voice, or off. Tap in options to change.
- **Haptics:** Pulse on damage, perfect parries, kills, and death; on by default.
- **Radar assist:** Off by default. When enabled, the title-screen options show a visual bearing dial/blips and add pings; it is hidden during calibration. It is optional, not required, and the resonance ring itself never gives a bearing.
- **Volume:** Cycles down in 25% steps and wraps back to full.
- **Reset progress:** Clears deaths, best wave, Codex, mastery, and run bargains. Accessibility settings persist.
- **Subtitles:** High-contrast text remains on screen for every spoken line.
- **Audio-first combat:** Enemy direction, windup, parry windows, and openings are conveyed through spatial sound.
- **Death rehearsal:** The fatal windup replays at 0.62x pitch through a 1.6 kHz low-pass. Align within 25 degrees and tap during the replay window to record the Codex; swipe up first if you want the run bargain. The replay stops after 4.8 seconds.

For a blind player: enable headphones, stay on the title until the spoken controls finish, then tap once. Hold the title at any time to hear the current narrator and haptics settings.

## Design

- Three waves, three hearts, one-handed play. A clean run is brief; deaths and Codex discovery stretch the session. Calibration precedes the first wave only.
- Enemy windups are spatialized: the side and distance of the sound tell you where the attack is coming from.
- Perfect parries (18 degrees / last 0.14 s while resonance-locked) stagger; standard parries through 35 degrees only stop the attack; 35-40 degrees glances and can send a Charger behind you.
- Death isolates the fatal windup, slows it, and lets you rehearse the aligned response before retrying; the rehearsal ends after 4.8 seconds.
- Permanent progression is knowledge: the Codex records discovered tells, and perfect unassisted parries mark mastery.
- Run bargains are temporary: Foresight, Grace, Iron, and Stillness persist across retries inside one run, add a finale harvest threat, and clear when a new run starts or you win.
- The Conductor finale has three phases: a drifting voice, a true/decoy harvest, and a final defense followed by a true-bearing swipe.
- All audio is synthesized at runtime. No external music or SFX assets.
- The UI is drawn with an original SVG art set ("obsidian & moonlight"): rig-safe engraved linework for the resonance ring and radar dial, shield HP pips, wave diamond pips, prompt glyphs, a title sigil, a victory laurel, and a full-screen vignette. Sources live in `Docs/UI/svg`, are rasterized at 2x by `bash Docs/UI/render.sh` (requires `rsvg-convert`), and load at runtime through `Assets/Resources/UI` with procedural-sprite fallbacks. Edge HUD sits under an Android safe-area inset so notches and overscan cannot clip it. Getting hit flares a short red arc from the attacker's direction on the ring; perfect parries bloom cyan; a half-second hold pauses the run (tap resumes, swipe down abandons).
- High-contrast text, large touch targets, and no required reading during combat.

## Build

- Unity `6000.6.7f1`, URP, new Input System, Android IL2CPP, ARM64.
- Open the project, then use **OMTK > Build Android APK** or run the same build from the Unity command line.
- Output: `Builds/Android/OneMoreTimeKnight.apk`.
- The checked-in build is debug-signed for jam installation and is not a store release.

## Known Limitations

- Debug-signed APK; no release keystore or AAB yet.
- Audio has been verified by RMS, headless suite, and editor playback, not by a human listening test on the target phone.
- The current Android APK includes the SVG UI art set, Tier 4 checks, and radar pings; it is installed and visually device-tested (title radar, calibration hiding, wave-1 blip, fatal marker) with the previous UI, but the new SVG HUD, safe-area insets, and flash layers are not yet device-verified. Ping audio is still unheard on the phone.
- Headless verification: `197/197` suite checks and a Victory smoke run at 3 HP in `31.01 s`; Linux build has 0 errors and 974 known warnings.
- Gyroscope feel, touch latency, TTS timing, haptics, and the full Finale/Victory flow still need a clean-state device pass.
- System TTS uses the phone's installed English voice; without one, the stylized synthesized fallback is not intelligible speech.
- The synthesized narrator mode is intentionally stylized; subtitles carry the exact wording.
- No localization, cloud save, or analytics.

## Credits

Design, code, procedural audio, and SVG UI art: Shoaib.

Made with Unity and the Universal Render Pipeline. No third-party art, music, or sound-effect assets are included.
