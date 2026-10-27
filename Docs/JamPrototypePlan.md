# One More Time, Knight — 48h Jam Plan

**GameJamPlus / solo / Android / themes: “One More Time” + “Something’s Off”**

## Pitch
A blind knight fights by sound. Every death, the Order whispers: “One more time.” But the voice guiding you is the monster. Turn your phone to face the sound, swipe to strike, and learn the tells before the loop learns you.

## Theme fit
- **One More Time:** 60–90 second runs, instant retry, attempt counter, narrator repeats the line, new lie each attempt.
- **Something’s Off:** the narrator is the final enemy; the “safe” bell is bait; at the end the voice becomes spatial and moves behind you.

## Core loop
1. Spawn in a dark arena with 3 hearts.
2. Monsters approach from 8 directions; each has a distinct mono loop.
3. Turn phone/body until the sound is centered; when the wind-up cue plays, swipe to strike.
4. Clear 5 monsters → next wave. Clear 3 waves → the narrator says “One more time.”
5. The voice becomes spatial behind you. Turn and strike it to break the loop. If you obey it, you die and retry.
6. Death/retry is instant; attempt counter and music layer change. The player wants one more try to hear what changed.

## Controls (one-handed, portrait)
- **Turn:** gyro yaw; recenter on retry.
- **Strike:** swipe up/forward anywhere; 0.25s cooldown.
- **Start/retry:** tap.
- No back-tap, no physical swing, no voice commands in the jam build.

## Scope
**Must:**
- One enemy type, 3 pitch/tempo variants; 3 waves; narrator twist; instant retry.
- Built-in 3D audio + behind low-pass; mono SFX; music layers.
- Black screen + resonance ring + subtitles; no directional visuals.
- Android build tested on the phone.

**Should:**
- Haptic pulse on wind-up; second enemy variant; title-screen sword silhouette.
- Simple audio menu (volume, recenter, sensitivity).

**Won’t:**
- 9-pose calibration, HRTF/Resonance Audio, full TalkBack, voice commands, 5 monster types, boss phases, 3D world, story campaign, online.

## Audio plan (owned assets)
- **Monsters:** Zombie Sound Pack (pitch -20/-30%, layer with Hit The Deck thuds).
- **Sword/parry:** Demo Ancient Magic Pack + SoundBits whooshes/impacts.
- **UI/earcons:** FREE Casual Game SFX Pack + Wind Chimes (resonance bell/safe cue).
- **Footsteps:** Footsteps - Essentials (mono).
- **Music:** Dark Atmospheric FREE TRACK Music Pack (adaptive layers; add a layer per retry).
- **Narrator:** record 8–10 lines; pitch/reverb; Android TTS fallback.
- **Missing:** true HRTF, monster VO library, narrator VO; approximate/record.
- Do not use Meta XR Audio SDK (Quest-only) or FMOD (build risk) for the jam.

## Tech plan
- **Pre-jam:** install Android Build Support + SDK/NDK + OpenJDK; make a blank Android build on the phone.
- **Player Settings:** Android, minSdk 31, targetSdk 36, IL2CPP, ARM64, portrait, Active Input Handling = Both.
- **Head:** `Input.gyro.attitude` yaw only; ignore pitch/roll; recenter on retry. No JNI/Madgwick.
- **Listener:** empty camera at origin; rotate yaw; audio sources in world.
- **Spatialization:** `spatialBlend=1`, `spread=0`, linear rolloff, no Doppler; behind low-pass cutoff 800–5000 Hz based on dot product; subtle volume dip.
- **Enemy:** spawn at angle/distance, move inward, wind-up 0.9s, attack; hit if facing ±35° and swipe in window.
- **AudioMixer:** Music/SFX/VO/UI; duck music under VO; limiter.
- **Visual:** black background, symmetrical ring pulsing with the mix; title/subtitles; no enemy direction.
- Build early and often; test on device every 4 hours.

## 48h schedule
**Pre-jam:** install Android, blank build, import audio, record VO, gyro test.

- **Fri 18:00–22:00:** sensor + listener + one sound + swipe + Android build.
- **Fri 22:00–02:00:** enemy + hit/parry + hearts + retry.
- **Sat 02:00–08:00:** sleep.
- **Sat 08:00–12:00:** behind-filter, waves, music layers.
- **Sat 12:00–14:00:** break.
- **Sat 14:00–18:00:** narrator, twist, UI, title.
- **Sat 18:00–20:00:** break.
- **Sat 20:00–00:00:** polish, variants, difficulty.
- **Sun 00:00–06:00:** sleep.
- **Sun 06:00–10:00:** device testing, bug fixes.
- **Sun 10:00–14:00:** final build, trailer/gifs, screenshots.
- **Sun 14:00–18:00:** submit build early; buffer.
- **Mon–Tue:** pitch deck + 60s trailer; submit by Tue 23:59.

## Risks / cuts
- **Gyro yaw unreliable:** fallback to touch-drag turn (still audio-first).
- **Behind-filter confusing:** widen hit angle, add haptic confirm.
- **Narrator twist doesn’t land:** end after 3 waves with the “One more time” loop; keep the twist as a post-credits sting.
- **Time short:** cut wave 3, second enemy, title 3D, haptics.
- **Never cut:** Android build, instant retry, audio tells, twist setup, submission.

## Pitch angle
“An audio-first mobile roguelite where the dungeon lies. Built for blind and sighted players. 60-second runs, instant retry, a narrator you learn to distrust. One more time.”
