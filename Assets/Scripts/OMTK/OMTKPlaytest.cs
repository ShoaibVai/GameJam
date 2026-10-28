using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace OMTK
{
    public class OMTKPlaytest : MonoBehaviour
    {
        private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;

        private OMTKGame game;
        private FieldInfo tokenField;
        private FieldInfo offbeatTokenField;
        private FieldInfo enemiesField;
        private FieldInfo finaleBeatField;
        private FieldInfo finaleTrueBearingField;
        private FieldInfo finaleThreatActiveField;
        private FieldInfo finaleThreatResolvedField;
        private FieldInfo finaleThreatWindowStartField;
        private FieldInfo finaleThreatWindowEndField;
        private FieldInfo finaleFinalDefendedField;
        private FieldInfo finaleFinalWindowStartField;
        private FieldInfo finaleFinalWindowEndField;
        private FieldInfo playerYawField;
        private GameState lastState = (GameState)(-1);
        private OMTKEnemy lastToken;
        private bool finaleSwiped;
        private bool deathSkipped;
        private bool finished;
        private bool suiteMode;
        private float runStartedAt;
        private int suitePassed;
        private int suiteFailed;

        private void Start()
        {
            game = Object.FindAnyObjectByType<OMTKGame>();
            if (game == null)
            {
                Debug.LogError("[PLAYTEST] no OMTKGame");
                enabled = false;
                return;
            }

            tokenField = typeof(OMTKGame).GetField("primaryToken", Flags);
            offbeatTokenField = typeof(OMTKGame).GetField("offbeatToken", Flags);
            enemiesField = typeof(OMTKGame).GetField("enemies", Flags);
            finaleBeatField = typeof(OMTKGame).GetField("finaleBeat", Flags);
            finaleTrueBearingField = typeof(OMTKGame).GetField("finaleTrueBearing", Flags);
            finaleThreatActiveField = typeof(OMTKGame).GetField("finaleThreatActive", Flags);
            finaleThreatResolvedField = typeof(OMTKGame).GetField("finaleThreatResolved", Flags);
            finaleThreatWindowStartField = typeof(OMTKGame).GetField("finaleThreatWindowStart", Flags);
            finaleThreatWindowEndField = typeof(OMTKGame).GetField("finaleThreatWindowEnd", Flags);
            finaleFinalDefendedField = typeof(OMTKGame).GetField("finaleFinalDefended", Flags);
            finaleFinalWindowStartField = typeof(OMTKGame).GetField("finaleFinalWindowStart", Flags);
            finaleFinalWindowEndField = typeof(OMTKGame).GetField("finaleFinalWindowEnd", Flags);
            playerYawField = typeof(OMTKGame).GetField("<PlayerYaw>k__BackingField", Flags);

            string[] args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-omtkPlaytestSuite")
                {
                    suiteMode = true;
                    break;
                }
            }

            if (suiteMode)
            {
                Time.timeScale = 0f;
                Debug.Log("[SUITE] started");
                StartCoroutine(RunSuite());
                return;
            }

            runStartedAt = Time.unscaledTime;
            Time.timeScale = 2f;
            Debug.Log("[PLAYTEST] started");
        }

        private void Update()
        {
            if (finished || suiteMode || game == null)
            {
                return;
            }

            if (game.State != lastState)
            {
                lastState = game.State;
                if (game.State != GameState.Death)
                {
                    deathSkipped = false;
                }

                Debug.Log("[PLAYTEST] state=" + game.State + " wave=" + game.Wave + " hp=" + game.Hp + "/" + game.MaxHp + " attempt=" + game.Attempt + " deaths=" + game.Deaths + " echoes=" + game.EchoMask);
            }

            if (game.Attempt > 6)
            {
                Debug.LogError("[PLAYTEST] too many attempts");
                finished = true;
                Time.timeScale = 1f;
                return;
            }

            switch (game.State)
            {
                case GameState.Title:
                    if (game.StateTime > 0.8f)
                    {
                        Tap("title");
                    }
                    break;
                case GameState.Intro:
                    if (game.StateTime > 0.8f)
                    {
                        Tap("intro");
                    }
                    break;
                case GameState.Calibration:
                    SetYaw(OMTKConfig.CalibrationAzimuth);
                    if (game.StateTime > 0.8f)
                    {
                        Tap("calibration");
                    }
                    break;
                case GameState.Playing:
                    UpdatePlaying();
                    break;
                case GameState.Interlude:
                    if (game.StateTime > 1.0f)
                    {
                        Tap("interlude");
                    }
                    break;
                case GameState.Death:
                    if (!deathSkipped && game.StateTime > 1.2f)
                    {
                        deathSkipped = true;
                        Call("SkipDeathRehearsal");
                        Debug.Log("[PLAYTEST] skip death rehearsal");
                    }
                    break;
                case GameState.Finale:
                    UpdateFinale();
                    break;
                case GameState.Victory:
                    if (!finished)
                    {
                        finished = true;
                        Time.timeScale = 1f;
                        Debug.Log("[PLAYTEST] VICTORY runSeconds=" + (Time.unscaledTime - runStartedAt).ToString("F1"));
                        StartCoroutine(QuitAfterVictory());
                    }
                    break;
            }
        }

        private IEnumerator QuitAfterVictory()
        {
            yield return new WaitForSecondsRealtime(0.5f);
            Application.Quit(0);
        }

        private IEnumerator RunSuite()
        {
            yield return null;
            PrepareSuite();

            TestRecenterTransitions();
            TestPerfectAndGrades();
            TestParryCooldowns();
            TestResonanceLock();
            yield return TestPrematureAndRecovery();
            TestShoveAndSlash();
            TestRadar();
            TestCalibration();
            TestTutorialDeath();
            TestProgress();
            TestVolumeAndGestures();
            TestTier2TempoAndClips();
            TestStalkerOscillationAndLock();
            TestChargerGlanceOvershoot();
            TestConjurerDecoy();
            yield return TestDualTokenTimeDip();
            TestOffbeatTokenScheduling();
            TestAttention();
            TestBearingsAndConductor();
            TestTensionAndCooldown();
            TestNarratorScheduling();
            TestTransitionCleanup();
            TestTier3Progression();
            TestTier3Death();
            yield return TestTier3Finale();

            Time.timeScale = 1f;
            Debug.Log("[SUITE] RESULT passed=" + suitePassed + " failed=" + suiteFailed);
            yield return new WaitForSecondsRealtime(0.2f);
            Application.Quit(suiteFailed == 0 ? 0 : 1);
        }

        private void PrepareSuite()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            OMTKConfig.RadarAssist = false;
            SetField("narratorMode", 2);
            game.Audio.SetNarratorMode(2);
            SetField("masterVolume", 1f);
            AudioListener.volume = 1f;
            SetField("CodexMask", 0);
            SetField("LearnedMask", 0);
            SetField("RunEchoMask", 0);
            SetField("ConductorResonance", 0);
            SetField("Deaths", 0);
            SetField("BestWave", 0);
            Call("RecalculateMaxHp");
            Call("StartRun");
            SetField("Attempt", 2);
            SetField("Wave", 1);
            SetField("Hp", 3);
            Call("SetState", GameState.Title);
            Call("ClearEnemies");
            Call("ClearTutorial");
            Call("ClearDeathRehearsal");
            SetField("fatalClip", null);
            SetField("primaryToken", null);
            SetField("offbeatToken", null);
            SetField("pendingToken", null);
            SetField("guardLockout", 0f);
        }

        private void TestRecenterTransitions()
        {
            SetYaw(123f);
            Call("StartRun");
            Check("recenter on StartRun", Near(game.PlayerYaw, 0f), game.PlayerYaw.ToString("F2"));

            SetField("Attempt", 2);
            SetYaw(45f);
            Call("BeginWave", 1);
            Check("recenter on BeginWave", Near(game.PlayerYaw, 0f), game.PlayerYaw.ToString("F2"));

            SetYaw(88f);
            Call("Retry");
            Check("recenter on Retry", Near(game.PlayerYaw, 0f), game.PlayerYaw.ToString("F2"));
            Check("recenter clip exists", game.Audio.RecenterClip != null);
            ClearAll();
        }

        private void TestPerfectAndGrades()
        {
            Check("perfect window", Near(game.PerfectWindow, OMTKConfig.PerfectWindow), game.PerfectWindow.ToString("F3"));
            Check("perfect angle", Near(game.PerfectParryAngle, OMTKConfig.PerfectParryAngle), game.PerfectParryAngle.ToString("F2"));
            Check("fixed max hp", game.MaxHp == 3, game.MaxHp.ToString());

            OMTKEnemy enemy = SetupEnemy(18f, 0.14f, true);
            Tap();
            Check("perfect at 18 degrees", enemy.IsStaggered, enemy.State.ToString());
            ClearAll();

            enemy = SetupEnemy(18.2f, 0.14f, true);
            Tap();
            Check("standard outside 18 degrees", enemy.State == EnemyState.Recover, enemy.State.ToString());
            ClearAll();

            enemy = SetupEnemy(0f, 0.141f, true);
            Tap();
            Check("standard outside perfect window", enemy.State == EnemyState.Recover, enemy.State.ToString());
            ClearAll();

            enemy = SetupEnemy(0f, 0.14f, false);
            Tap();
            Check("perfect requires resonance lock", enemy.State == EnemyState.Recover, enemy.State.ToString());
            ClearAll();

            enemy = SetupEnemy(35f, 0.2f, false);
            Tap();
            Check("standard at 35 degrees", enemy.State == EnemyState.Recover && Near(GetEnemyFloat(enemy, "recoverRemaining"), 0.42f), enemy.State.ToString());
            ClearAll();

            enemy = SetupEnemy(35.5f, 0.2f, false);
            Tap();
            Check("glancing beyond 35 degrees", Near(GetEnemyFloat(enemy, "recoverRemaining"), 0.34f), GetEnemyFloat(enemy, "recoverRemaining").ToString("F2"));
            ClearAll();

            enemy = SetupEnemy(40.5f, 0.2f, false);
            Tap();
            Check("off-axis whiff beyond 40 degrees", enemy.IsWindingUp, enemy.State.ToString());
            ClearAll();

            SetField("RunEchoMask", OMTKConfig.EchoBit(EchoId.Grace));
            enemy = SetupEnemy(25f, 0.21f, true);
            Tap();
            Check("grace perfect at active angle boundary", enemy.IsStaggered, enemy.State.ToString());
            ClearAll();

            SetField("RunEchoMask", OMTKConfig.EchoBit(EchoId.Grace));
            enemy = SetupEnemy(25.5f, 0.21f, true);
            Tap();
            Check("grace boundary rejects outside angle", enemy.State == EnemyState.Recover, enemy.State.ToString());
            ClearAll();
            SetField("RunEchoMask", 0);
        }

        private void TestParryCooldowns()
        {
            for (int mode = 0; mode <= 2; mode++)
            {
                SetupPlaying();
                SetField("narratorMode", mode);
                game.Audio.SetNarratorMode(mode);
                OMTKEnemy enemy = AddEnemy(EnemyVariant.Stalker, 0f);
                enemy.PlaceAtDistance(2.35f);
                enemy.BeginScriptedWindup(0.2f);
                SetField("primaryToken", enemy);
                SetYaw(enemy.Azimuth);
                SetEnemyLock(enemy, true);
                Tap();
                Check("standard cooldown mode " + mode, Near(GetFloat("windupCooldown"), OMTKConfig.AttackCooldown * OMTKConfig.StandardParryCooldownScale, 0.001f), GetFloat("windupCooldown").ToString("F3"));
                ClearAll();
            }

            SetField("narratorMode", 2);
            game.Audio.SetNarratorMode(2);
            SetupPlaying();
            OMTKEnemy perfect = AddEnemy(EnemyVariant.Stalker, 0f);
            perfect.PlaceAtDistance(2.35f);
            perfect.BeginScriptedWindup(0.14f);
            SetField("primaryToken", perfect);
            SetYaw(perfect.Azimuth);
            SetEnemyLock(perfect, true);
            Tap();
            Check("perfect cooldown normalized", Near(GetFloat("windupCooldown"), OMTKConfig.AttackCooldown, 0.001f), GetFloat("windupCooldown").ToString("F3"));
            ClearAll();
        }

        private void TestResonanceLock()
        {
            SetupPlaying();
            OMTKEnemy enemy = AddEnemy(EnemyVariant.Charger, 0f);
            enemy.PlaceAtDistance(2.35f);
            SetYaw(enemy.Azimuth);
            enemy.Tick(0.16f, game.PlayerYaw, game.PerfectWindow);
            enemy.Tick(0.15f, game.PlayerYaw, game.PerfectWindow);
            Check("resonance lock after 0.3 seconds", enemy.IsResonanceLocked, enemy.LockProgress.ToString("F2"));

            SetYaw(enemy.Azimuth + 50f);
            enemy.Tick(0.01f, game.PlayerYaw, game.PerfectWindow);
            Check("resonance lock drops outside cone", !enemy.IsResonanceLocked);

            SetYaw(enemy.Azimuth);
            enemy.Tick(0.31f, game.PlayerYaw, game.PerfectWindow);
            enemy.BeginScriptedWindup(0.14f);
            SetField("primaryToken", enemy);
            Tap();
            Check("locked perfect parry", enemy.IsStaggered, enemy.State.ToString());
            ClearAll();
        }

        private IEnumerator TestPrematureAndRecovery()
        {
            SetupPlaying();
            OMTKEnemy enemy = AddEnemy(EnemyVariant.Stalker, 0f);
            enemy.PlaceAtDistance(2.35f);
            enemy.BeginScriptedWindup(0.5f);
            SetField("primaryToken", enemy);
            SetYaw(enemy.Azimuth);
            SetField("guardLockout", 0f);
            Tap();
            Check("premature tap lockout", GetFloat("guardLockout") > 0.4f, GetFloat("guardLockout").ToString("F2"));
            Check("tinnitus filter engaged", Near(GetMusicCutoff(), OMTKConfig.TinnitusFrequency), GetMusicCutoff().ToString("F0"));

            Call("SetState", GameState.Title);
            Time.timeScale = 1f;
            yield return new WaitForSecondsRealtime(0.95f);
            Check("guard lockout recovers", GetFloat("guardLockout") <= 0f, GetFloat("guardLockout").ToString("F2"));
            Check("audio filter recovers", GetMusicCutoff() > 20000f, GetMusicCutoff().ToString("F0"));
            Time.timeScale = 0f;
            ClearAll();
        }

        private void TestShoveAndSlash()
        {
            SetupPlaying();
            OMTKEnemy enemy = SetupEnemy(0f, 0.14f, true);
            Tap();
            Check("perfect staggers before shove", enemy.IsStaggered, enemy.State.ToString());
            Tap();
            Check("tap shoves instead of killing", enemy.IsAlive && !enemy.IsStaggered && enemy.Distance > 7f, enemy.Distance.ToString("F2"));
            Check("shove distance", Near(enemy.Distance, 7.4f), enemy.Distance.ToString("F2"));
            ClearAll();

            SetupPlaying();
            enemy = SetupEnemy(0f, 0.14f, true);
            Tap();
            enemy.BeginScriptedWindup(0.14f);
            enemy.Parry(ParryGrade.Perfect);
            Check("enemy ready for slash", enemy.IsStaggered, enemy.State.ToString());
            Swipe();
            Check("on-axis swipe kills staggered enemy", !enemy.IsAlive, enemy.State.ToString());
            ClearAll();

            SetupPlaying();
            enemy = SetupEnemy(0f, 0.14f, true);
            Tap();
            enemy.BeginScriptedWindup(0.14f);
            enemy.Parry(ParryGrade.Perfect);
            SetYaw(enemy.Azimuth + 50f);
            Swipe();
            Check("off-axis swipe cannot bypass check", enemy.IsAlive && enemy.IsStaggered, enemy.State.ToString());
            ClearAll();

            SetupPlaying();
            AddEnemy(EnemyVariant.Stalker, 90f);
            float yaw = 77f;
            SetYaw(yaw);
            Swipe();
            Check("swipe does not change yaw", Near(game.PlayerYaw, yaw), game.PlayerYaw.ToString("F2"));
            ClearAll();
        }

        private void TestRadar()
        {
            PlayerPrefs.DeleteKey("OMTK_RadarAssist");
            OMTKConfig.RadarAssist = false;
            Call("LoadAccessibility");
            Check("radar defaults off", !OMTKConfig.RadarAssist);

            Call("SetState", GameState.Playing);
            AddEnemy(EnemyVariant.Stalker, 90f);
            SetField("radarVisible", true);
            Image dial = GetField("radarDial") as Image;
            Text readout = GetField("radarReadout") as Text;
            Image echoRing = GetField("ring") as Image;
            if (dial != null)
            {
                dial.gameObject.SetActive(true);
            }
            if (readout != null)
            {
                readout.gameObject.SetActive(true);
            }

            Call("UpdateRadar", 0.016f);
            Check("radar hidden when off", dial != null && !dial.gameObject.activeSelf);
            Check("radar readout hidden when off", readout != null && !readout.gameObject.activeSelf);
            Transform[] blips = GetField("radarBlipRoots") as Transform[];
            bool blipsHidden = true;
            if (blips != null)
            {
                for (int i = 0; i < blips.Length; i++)
                {
                    if (blips[i] != null && blips[i].gameObject.activeSelf)
                    {
                        blipsHidden = false;
                    }
                }
            }

            Check("radar blips hidden when off", blipsHidden);
            Check("echo ring stays non-directional", echoRing != null && echoRing.gameObject.activeSelf && Quaternion.Angle(echoRing.rectTransform.localRotation, Quaternion.identity) < 0.1f);

            Call("ToggleRadar");
            Check("radar toggle persists on", OMTKConfig.RadarAssist && PlayerPrefs.GetInt("OMTK_RadarAssist", 0) == 1);
            OMTKConfig.RadarAssist = false;
            Call("LoadAccessibility");
            Check("radar preference reloads", OMTKConfig.RadarAssist);
            Call("ToggleRadar");
            PlayerPrefs.DeleteKey("OMTK_RadarAssist");
            PlayerPrefs.Save();
            OMTKConfig.RadarAssist = false;
            Call("LoadAccessibility");

            Call("SetState", GameState.Playing);
            AddEnemy(EnemyVariant.Stalker, 90f);
            OMTKConfig.RadarAssist = true;
            Call("UpdateRadar", 0.016f);
            Call("SetState", GameState.Calibration);
            Call("UpdateRadar", 0.016f);
            Check("radar hidden during calibration", dial != null && !dial.gameObject.activeSelf);
            OMTKConfig.RadarAssist = false;
            PlayerPrefs.DeleteKey("OMTK_RadarAssist");
            PlayerPrefs.Save();
            ClearAll();
        }

        private void TestCalibration()
        {
            Call("StartRun");
            Call("StartCalibration");
            SetYaw(0f);
            Tap();
            Check("calibration rejects misalignment", game.State == GameState.Calibration && !GetBool("calibrationDone"), game.State.ToString());
            SetYaw(OMTKConfig.CalibrationAzimuth);
            Tap();
            Check("calibration accepts aligned tap", game.State == GameState.Playing && GetBool("calibrationDone"), game.State.ToString());
            ClearAll();

            Call("StartRun");
            Call("StartCalibration");
            SetField("<StateTime>k__BackingField", OMTKConfig.CalibrationFallbackDuration + 0.1f);
            Call("UpdateCalibration");
            Check("calibration fallback exits", game.State == GameState.Playing && GetBool("calibrationDone"), game.State.ToString());
            ClearAll();
        }

        private void TestTutorialDeath()
        {
            Call("StartRun");
            SetField("Attempt", 1);
            Call("BeginWave", 1);
            Call("UpdateTutorialDeath", 3.1f);
            OMTKEnemy killer = GetField("tutorialKiller") as OMTKEnemy;
            Check("tutorial windup starts", GetBool("tutorialWindupStarted") && killer != null && GetField("primaryToken") == killer);
            int hp = game.Hp;
            if (killer != null)
            {
                SetYaw(killer.Azimuth);
            }
            Tap();
            Check("tutorial blow cannot be parried", game.Hp == hp && killer != null && !killer.IsStaggered, game.Hp + "/" + hp);
            if (killer != null)
            {
                Call("EnemyAttack", killer);
            }
            Check("tutorial first death is deterministic", game.State == GameState.Death && game.Hp == 0, game.State.ToString());
            Check("tutorial death grants no echo", GetBool("currentDeathTutorial") && game.EchoMask == 0, game.EchoMask.ToString());
            Call("Retry");
            ClearAll();
        }

        private void TestProgress()
        {
            SetField("EchoMask", 15);
            Call("RecalculateMaxHp");
            Check("legacy echo mask does not raise max hp", game.MaxHp == 3, game.MaxHp.ToString());
            Check("legacy echo has no combat effect", !game.HasEcho(EchoId.Grace));

            PlayerPrefs.SetInt("OMTK_Deaths", 10);
            PlayerPrefs.SetInt("OMTK_EchoMask", 15);
            PlayerPrefs.Save();
            Call("LoadProgress");
            Check("legacy echo key cleared", game.EchoMask == 0 && PlayerPrefs.GetInt("OMTK_EchoMask", 999) == 999, game.EchoMask.ToString());
            Check("loaded max hp fixed", game.MaxHp == 3, game.MaxHp.ToString());
            Check("no new echo unlock", Call("UnlockNextEcho") == null);
        }

        private void TestVolumeAndGestures()
        {
            SetField("masterVolume", 1f);
            AudioListener.volume = 1f;
            PlayerPrefs.DeleteKey("OMTK_Volume");
            Call("CycleVolume");
            Check("volume cycles to 75", Near(AudioListener.volume, 0.75f, 0.01f), AudioListener.volume.ToString("F2"));
            Check("volume persists", Near(PlayerPrefs.GetFloat("OMTK_Volume", -1f), 0.75f, 0.01f));
            Call("LoadAccessibility");
            Check("volume reloads", Near(GetFloat("masterVolume"), 0.75f, 0.01f), GetFloat("masterVolume").ToString("F2"));
            Call("CycleVolume");
            Call("CycleVolume");
            Call("CycleVolume");
            Check("volume reaches zero", AudioListener.volume <= 0.001f, AudioListener.volume.ToString("F2"));
            Call("CycleVolume");
            Check("volume wraps to full", Near(AudioListener.volume, 1f, 0.01f), AudioListener.volume.ToString("F2"));

            SetField("lastCueClip", null);
            SetYaw(42f);
            Call("RepeatLastCueAndHeading");
            Text subtitle = GetField("subtitleText") as Text;
            Check("two-finger tap reports heading", subtitle != null && subtitle.text.Contains("HEADING 042"), subtitle == null ? "no text" : subtitle.text);
            Check("heading repeat does not recenter", Near(game.PlayerYaw, 42f), game.PlayerYaw.ToString("F2"));
            Call("RecenterYawAndReport");
            Check("double-tap recenters yaw", Near(game.PlayerYaw, 0f), game.PlayerYaw.ToString("F2"));
            Check("double-tap reports hp and wave", subtitle != null && subtitle.text.Contains("RECENTERED") && subtitle.text.Contains("HP"), subtitle == null ? "no text" : subtitle.text);

            PlayerPrefs.DeleteKey("OMTK_Volume");
            PlayerPrefs.Save();
            SetField("masterVolume", 1f);
            AudioListener.volume = 1f;
        }

        private void TestTier2TempoAndClips()
        {
            SetupPlaying();
            OMTKEnemy charger = AddEnemy(EnemyVariant.Charger, 0f);
            charger.PlaceAtDistance(OMTKConfig.ChargerNearDistance);
            Check("charger near tempo", Near(charger.PresenceTempo, OMTKConfig.ChargerNearTempo, 0.05f), charger.PresenceTempo.ToString("F2"));
            charger.PlaceAtDistance(OMTKConfig.ChargerFarDistance);
            Check("charger far tempo", Near(charger.PresenceTempo, OMTKConfig.ChargerFarTempo, 0.05f), charger.PresenceTempo.ToString("F2"));
            charger.PlaceAtDistance((OMTKConfig.ChargerNearDistance + OMTKConfig.ChargerFarDistance) * 0.5f);
            Check("charger midpoint tempo linear", Near(charger.PresenceTempo, (OMTKConfig.ChargerNearTempo + OMTKConfig.ChargerFarTempo) * 0.5f, 0.05f), charger.PresenceTempo.ToString("F2"));
            Check("charger pulse clip exists", game.Audio.ChargerPulseClip != null);
            Check("decoy windup clip exists", game.Audio.DecoyWindupClip != null);
            Check("decoy approach clip exists", game.Audio.DecoyApproachClip != null);
            Check("charger overshoot distance config", Near(OMTKConfig.ChargerOvershootDistance, 4.5f));
            Check("dual token threshold config", Near(OMTKConfig.MinEnemySeparation, 85f));
            ClearAll();
        }

        private void TestStalkerOscillationAndLock()
        {
            SetupPlaying();
            OMTKEnemy stalker = AddEnemy(EnemyVariant.Stalker, 100f);
            stalker.PlaceAtDistance(9f);
            float approachDistance = stalker.Distance;
            SetEnemyField(stalker, "phase", -Time.time * 0.55f + Mathf.PI * 0.5f);
            stalker.Tick(0.5f, 0f, game.PerfectWindow);
            Check("stalker closes during approach", stalker.Distance < approachDistance, stalker.Distance.ToString("F2"));
            float baseAzimuth = GetEnemyFloat(stalker, "baseAzimuth");
            Check("stalker oscillates positive", Near(Mathf.DeltaAngle(baseAzimuth, stalker.Azimuth), OMTKConfig.StalkerOscillation, 0.5f), stalker.Azimuth.ToString("F2"));

            SetEnemyField(stalker, "phase", -Time.time * 0.55f - Mathf.PI * 0.5f);
            stalker.Tick(0f, 0f, game.PerfectWindow);
            Check("stalker oscillates negative", Near(Mathf.DeltaAngle(baseAzimuth, stalker.Azimuth), -OMTKConfig.StalkerOscillation, 0.5f), stalker.Azimuth.ToString("F2"));

            stalker.PlaceAtDistance(2.35f);
            stalker.BeginWindup();
            float lockedAzimuth = stalker.Azimuth;
            SetEnemyField(stalker, "phase", -Time.time * 0.55f + Mathf.PI * 0.5f);
            stalker.Tick(0.1f, 0f, game.PerfectWindow);
            Check("stalker locks bearing during windup", Near(stalker.Azimuth, lockedAzimuth), stalker.Azimuth.ToString("F2"));
            ClearAll();
        }

        private void TestChargerGlanceOvershoot()
        {
            SetupPlaying();
            OMTKEnemy charger = AddEnemy(EnemyVariant.Charger, 0f);
            charger.PlaceAtDistance(2.35f);
            charger.BeginScriptedWindup(0.2f);
            SetField("primaryToken", charger);
            SetYaw(charger.Azimuth + 37f);
            Tap();
            Check("charger glancing enters approach", charger.State == EnemyState.Approach, charger.State.ToString());
            Check("charger glancing overshoots distance", Near(charger.Distance, OMTKConfig.ChargerOvershootDistance), charger.Distance.ToString("F2"));
            Check("charger glancing reverses bearing", Near(Mathf.Abs(Mathf.DeltaAngle(0f, charger.Azimuth)), 180f, 0.1f), charger.Azimuth.ToString("F2"));
            Check("charger recharges from behind", charger.State == EnemyState.Approach && !charger.AtTarget, charger.State.ToString());
            charger.Tick(0.25f, 0f, game.PerfectWindow);
            Check("charger rear approach closes", charger.Distance < OMTKConfig.ChargerOvershootDistance, charger.Distance.ToString("F2"));
            ClearAll();
        }

        private void TestConjurerDecoy()
        {
            SetupPlaying();
            OMTKEnemy conjurer = AddEnemy(EnemyVariant.Conjurer, 0f);
            conjurer.PlaceAtDistance(2.35f);
            Call("QueueWindup", conjurer, false);
            Check("conjurer starts decoy", GetField("pendingToken") == conjurer && conjurer.DecoyActive, conjurer.DecoyActive.ToString());
            Check("conjurer decoy lead configured", Near(GetFloat("pendingLead"), OMTKConfig.ConjurerDecoyLead), GetFloat("pendingLead").ToString("F2"));
            Check("conjurer decoy is off-axis", Near(Mathf.Abs(Mathf.DeltaAngle(conjurer.Azimuth, conjurer.DecoyAzimuth)), 90f, 0.1f), conjurer.DecoyAzimuth.ToString("F2"));
            Call("UpdateTokenScheduler", OMTKConfig.ConjurerDecoyLead - 0.01f);
            Check("conjurer decoy precedes real windup", GetField("primaryToken") == null && GetField("pendingToken") == conjurer && !conjurer.IsWindingUp);
            Call("UpdateTokenScheduler", 0.02f);
            Check("conjurer decoy launches real windup", GetField("primaryToken") == conjurer && conjurer.IsWindingUp, conjurer.State.ToString());
            Check("conjurer decoy stops on real windup", !conjurer.DecoyActive);
            if (game.Audio.WindupConjurer != null && game.Audio.DecoyWindupClip != null)
            {
                float realLow = MagnitudeAt(game.Audio.WindupConjurer, 68f) + MagnitudeAt(game.Audio.WindupConjurer, 136f);
                float decoyLow = MagnitudeAt(game.Audio.DecoyWindupClip, 68f) + MagnitudeAt(game.Audio.DecoyWindupClip, 136f);
                float decoyHigh = MagnitudeAt(game.Audio.DecoyWindupClip, 980f);
                Check("conjurer decoy omits sub-bass", decoyLow < realLow * 0.2f, decoyLow.ToString("F4") + "/" + realLow.ToString("F4"));
                Check("conjurer decoy keeps upper tone", decoyHigh > 0.01f, decoyHigh.ToString("F4"));
            }

            SetYaw(conjurer.Azimuth + 90f);
            SetField("guardLockout", 0f);
            Tap();
            Check("conjurer decoy tap triggers tinnitus", GetFloat("guardLockout") > 0.4f, GetFloat("guardLockout").ToString("F2"));
            game.Audio.StopTinnitus();
            ClearAll();
        }

        private IEnumerator TestDualTokenTimeDip()
        {
            SetupPlaying();
            SetField("Attempt", 3);
            SetField("Wave", 3);
            OMTKEnemy primary = AddEnemy(EnemyVariant.Stalker, 0f);
            OMTKEnemy offbeat = AddEnemy(EnemyVariant.Stalker, 90f);
            primary.PlaceAtDistance(2.35f);
            offbeat.PlaceAtDistance(2.35f);
            primary.BeginScriptedWindup(0.14f);
            offbeat.BeginScriptedWindup(0.6f);
            SetField("primaryToken", primary);
            SetField("offbeatToken", offbeat);
            SetEnemyLock(primary, true);
            SetYaw(primary.Azimuth);
            Tap();
            Check("dual token primary perfect", primary.IsStaggered, primary.State.ToString());
            Check("dual token offbeat retained", GetField("offbeatToken") == offbeat);
            Check("offbeat time dip starts", GetBool("timeDipActive") && Near(Time.timeScale, OMTKConfig.OffbeatTimeScale, 0.001f), Time.timeScale.ToString("F2"));
            yield return new WaitForSecondsRealtime(OMTKConfig.OffbeatTimeDipDuration + 0.15f);
            Check("offbeat time dip restores", !GetBool("timeDipActive") && Near(Time.timeScale, 1f), Time.timeScale.ToString("F2"));
            Time.timeScale = 0f;
            ClearAll();
        }

        private void TestOffbeatTokenScheduling()
        {
            SetupPlaying();
            SetField("Attempt", 3);
            SetField("Wave", 3);
            OMTKEnemy primary = AddEnemy(EnemyVariant.Stalker, 0f);
            OMTKEnemy offbeat = AddEnemy(EnemyVariant.Stalker, 90f);
            primary.PlaceAtDistance(2.35f);
            offbeat.PlaceAtDistance(2.35f);
            Call("LaunchWindup", primary, false);
            Check("offbeat delay configured", Near(GetFloat("offbeatDelay"), OMTKConfig.OffbeatWindupDelay), GetFloat("offbeatDelay").ToString("F2"));
            Call("UpdateTokenScheduler", OMTKConfig.OffbeatWindupDelay - 0.05f);
            Check("offbeat waits for delay", Near(GetFloat("offbeatDelay"), 0.05f) && GetField("offbeatToken") == null, GetFloat("offbeatDelay").ToString("F2"));
            Call("UpdateTokenScheduler", 0.06f);
            Check("offbeat launches after delay", GetField("offbeatToken") == offbeat && offbeat.IsWindingUp, offbeat.State.ToString());
            ClearAll();
        }

        private void TestAttention()
        {
            SetupPlaying();
            OMTKEnemy attended = AddEnemy(EnemyVariant.Stalker, 0f);
            OMTKEnemy unattended = AddEnemy(EnemyVariant.Stalker, 90f);
            SetYaw(0f);
            Call("UpdateAttention");
            Check("attended enemy target", Near(GetEnemyFloat(attended, "attentionTarget"), 1f), GetEnemyFloat(attended, "attentionTarget").ToString("F2"));
            Check("unattended enemy target", Near(GetEnemyFloat(unattended, "attentionTarget"), 0.35f), GetEnemyFloat(unattended, "attentionTarget").ToString("F2"));
            attended.Tick(0.5f, 0f, game.PerfectWindow);
            unattended.Tick(0.5f, 0f, game.PerfectWindow);
            Check("attended clarity stays high", attended.Attention > 0.9f, attended.Attention.ToString("F2"));
            Check("unattended clarity is low", unattended.Attention < 0.5f, unattended.Attention.ToString("F2"));
            ClearAll();
        }

        private void TestBearingsAndConductor()
        {
            Call("StartRun");
            SetField("Attempt", 1);
            Call("BeginWave", 1);
            IList onboarding = GetEnemies();
            OMTKEnemy tutorial = onboarding != null && onboarding.Count > 0 ? onboarding[0] as OMTKEnemy : null;
            Check("attempt one wave one killer bearing", tutorial != null && Near(tutorial.Azimuth, 150f, 0.1f), tutorial == null ? "missing" : tutorial.Azimuth.ToString("F1"));

            Call("BeginWave", 2);
            IList secondWave = GetEnemies();
            OMTKEnemy charger = secondWave != null && secondWave.Count > 0 ? secondWave[0] as OMTKEnemy : null;
            Check("attempt one wave two charger bearing", charger != null && charger.Variant == EnemyVariant.Charger && Near(charger.Azimuth, -74f, 0.1f), charger == null ? "missing" : charger.Azimuth.ToString("F1") + "/" + charger.Variant);

            Call("BeginWave", 3);
            IList pair = GetEnemies();
            Check("fixed wave three pair", pair != null && pair.Count == 2, pair == null ? "no list" : pair.Count.ToString());
            if (pair != null && pair.Count >= 2)
            {
                OMTKEnemy first = pair[0] as OMTKEnemy;
                OMTKEnemy second = pair[1] as OMTKEnemy;
                Check("fixed wave three bearings", first != null && second != null && Near(first.Azimuth, 142f, 0.1f) && Near(second.Azimuth, -38f, 0.1f), first == null || second == null ? "missing" : first.Azimuth.ToString("F1") + "/" + second.Azimuth.ToString("F1"));
            }

            ClearAll();
            Call("StartRun");
            SetField("Attempt", 3);
            Call("BeginWave", 3);
            IList trio = GetEnemies();
            Check("attempt three adds voice", trio != null && trio.Count == 3, trio == null ? "no list" : trio.Count.ToString());
            Check("attempt three pair separation", trio != null && trio.Count >= 2 && PairSeparationAtLeast(trio, OMTKConfig.MinEnemySeparation), trio == null ? "no list" : trio.Count.ToString());

            Call("StartRun");
            SetField("Attempt", 2);
            Call("RollConductorEdit");
            object edit = GetField("conductorEdit");
            Check("conductor edit appears on retry", edit != null && System.Convert.ToInt32(edit) > 0, edit == null ? "none" : edit.ToString());
            object hint = Call("ConductorHint");
            Check("conductor edit has hint", hint is string && !string.IsNullOrEmpty((string)hint), hint == null ? "none" : hint.ToString());
            ClearAll();
        }

        private void TestTensionAndCooldown()
        {
            Call("StartRun");
            SetField("Attempt", 2);
            Call("BeginWave", 1);
            Check("tension off in early waves", !game.Audio.TensionActive, game.Audio.TensionActive.ToString());
            Call("BeginWave", 3);
            Check("tension on in wave three", game.Audio.TensionActive, game.Audio.TensionActive.ToString());
            Call("BeginWave", 2);
            Check("tension clears before wave three", !game.Audio.TensionActive, game.Audio.TensionActive.ToString());

            SetupPlaying();
            OMTKEnemy attacker = AddEnemy(EnemyVariant.Stalker, 0f);
            SetField("windupCooldown", 0f);
            int hp = game.Hp;
            Call("EnemyAttack", attacker);
            Check("attack cooldown normalized", Near(GetFloat("windupCooldown"), OMTKConfig.AttackCooldown), GetFloat("windupCooldown").ToString("F2"));
            Check("attack consumes one heart", game.Hp == hp - 1, game.Hp + "/" + hp);
            ClearAll();
        }

        private void TestNarratorScheduling()
        {
            SetupPlaying();
            SetField("narratorMode", 1);
            game.Audio.SetNarratorMode(1);
            Call("BeginWave", 2);
            OMTKEnemy target = AddEnemy(EnemyVariant.Stalker, 0f);
            target.PlaceAtDistance(2.35f);
            SetField("waitForSpeech", true);
            game.Audio.Speak("Wave test narration.");
            Check("wave narration is active", game.Audio.IsSpeechActive, game.Audio.IsSpeechActive.ToString());
            Call("UpdateTokenScheduler", 3f);
            Check("wave waits for narration", GetField("primaryToken") == null && GetBool("waitForSpeech"));

            game.Audio.StopVoice();
            Call("UpdateTokenScheduler", 0.01f);
            Check("wave speech wait clears", !GetBool("waitForSpeech"), GetBool("waitForSpeech").ToString());
            Call("UpdateTokenScheduler", OMTKConfig.WaveStartCooldown + 0.1f);
            Check("wave launches after narration", GetField("primaryToken") == target, GetField("primaryToken") == null ? "none" : "other");
            ClearAll();
            SetField("narratorMode", 2);
            game.Audio.SetNarratorMode(2);
        }

        private void TestTransitionCleanup()
        {
            SetupPlaying();
            OMTKEnemy primary = AddEnemy(EnemyVariant.Stalker, 0f);
            OMTKEnemy offbeat = AddEnemy(EnemyVariant.Stalker, 90f);
            primary.PlaceAtDistance(2.35f);
            offbeat.PlaceAtDistance(2.35f);
            Call("LaunchWindup", primary, false);
            SetField("offbeatToken", offbeat);
            SetField("waitForSpeech", true);
            SetField("timeDipActive", true);
            Time.timeScale = OMTKConfig.OffbeatTimeScale;
            game.Audio.PlayTinnitus();
            Call("StartDeath", primary);
            Check("transition clears attack tokens", GetField("primaryToken") == null && GetField("offbeatToken") == null, "tokens stuck");
            Check("transition restores time scale", Near(Time.timeScale, 1f), Time.timeScale.ToString("F2"));
            Check("transition restores audio filter", GetMusicCutoff() > 20000f, GetMusicCutoff().ToString("F0"));
            Time.timeScale = 0f;
            Call("Retry");
            Check("retry clears pending speech", !GetBool("waitForSpeech"), GetBool("waitForSpeech").ToString());
            ClearAll();
        }

        private void TestTier3Progression()
        {
            Call("StartRun");
            SetField("Attempt", 2);
            SetField("CodexMask", 0);
            SetField("LearnedMask", 0);
            Call("ResetTrainingChimes");
            int bit = OMTKConfig.CodexBit(EnemyVariant.Stalker);
            SetField("CodexMask", bit);
            Check("codex bit exposed", game.CodexMask == bit, game.CodexMask.ToString());
            Check("training chime first charge", game.TryTrainingChime(EnemyVariant.Stalker, Vector3.forward));
            Check("training chime second charge", game.TryTrainingChime(EnemyVariant.Stalker, Vector3.forward));
            Check("training chime third charge", game.TryTrainingChime(EnemyVariant.Stalker, Vector3.forward));
            Check("training chime stops after charges", !game.TryTrainingChime(EnemyVariant.Stalker, Vector3.forward));
            SetField("LearnedMask", bit);
            Check("learned chime stops", !game.TryTrainingChime(EnemyVariant.Stalker, Vector3.forward));

            Call("StartRun");
            SetField("Attempt", 2);
            SetField("CodexMask", bit);
            SetField("LearnedMask", 0);
            SetupPlaying();
            OMTKEnemy enemy = AddEnemy(EnemyVariant.Stalker, 0f);
            enemy.PlaceAtDistance(2.35f);
            enemy.BeginScriptedWindup(0.3f);
            SetField("primaryToken", enemy);
            SetYaw(enemy.Azimuth);
            Tap();
            Check("standard parry does not master", (game.LearnedMask & bit) == 0, game.LearnedMask.ToString());

            SetupPlaying();
            enemy = AddEnemy(EnemyVariant.Stalker, 0f);
            enemy.PlaceAtDistance(2.35f);
            enemy.BeginScriptedWindup(0.14f);
            SetField("primaryToken", enemy);
            SetYaw(enemy.Azimuth);
            SetEnemyLock(enemy, true);
            Tap();
            Check("perfect parry masters codex", (game.LearnedMask & bit) != 0, game.LearnedMask.ToString());
            Check("learned chime remains silent", !game.TryTrainingChime(EnemyVariant.Stalker, Vector3.forward));

            SetField("RunEchoMask", OMTKConfig.EchoBit(EchoId.Grace));
            SetField("ConductorResonance", 2);
            Check("run grace widens perfect window", Near(game.PerfectWindow, OMTKConfig.GracePerfectWindow), game.PerfectWindow.ToString("F3"));
            Check("run grace widens perfect angle", Near(game.PerfectParryAngle, OMTKConfig.GracePerfectParryAngle), game.PerfectParryAngle.ToString("F2"));
            SetField("RunEchoMask", 0);
            Check("grace expires with run mask", Near(game.PerfectWindow, OMTKConfig.PerfectWindow), game.PerfectWindow.ToString("F3"));

            SetField("CodexMask", 3);
            SetField("LearnedMask", 1);
            Call("SaveProgress");
            SetField("CodexMask", 0);
            SetField("LearnedMask", 0);
            Call("LoadProgress");
            Check("codex persists", (game.CodexMask & 3) == 3, game.CodexMask.ToString());
            Check("mastery persists", (game.LearnedMask & 1) == 1, game.LearnedMask.ToString());
            Check("codex does not change max hp", game.MaxHp == 3, game.MaxHp.ToString());

            int deathsBefore = game.Deaths;
            for (int i = 0; i < 3; i++)
            {
                SetupPlaying();
                OMTKEnemy repeatKiller = AddEnemy(EnemyVariant.Stalker, 10f + i * 20f);
                repeatKiller.PlaceAtDistance(2.35f);
                Call("StartDeath", repeatKiller);
                Call("Retry");
            }

            Check("repeated deaths do not inflate hp", game.MaxHp == 3 && Near(game.PerfectWindow, OMTKConfig.PerfectWindow), game.MaxHp + "/" + game.PerfectWindow.ToString("F3"));
            Check("repeated deaths do not grant run power", game.RunEchoMask == 0 && game.ConductorResonance == 0, game.RunEchoMask + "/" + game.ConductorResonance);
            Check("repeated deaths still count deaths", game.Deaths == deathsBefore + 3, game.Deaths + "/" + deathsBefore);

            SetField("RunEchoMask", OMTKConfig.EchoBit(EchoId.Grace));
            SetField("ConductorResonance", 2);
            SetField("Deaths", 4);
            SetField("BestWave", 3);
            Call("ResetProgress");
            Check("reset clears death stats", game.Deaths == 0 && game.BestWave == 0, game.Deaths + "/" + game.BestWave);
            Check("reset clears codex", game.CodexMask == 0, game.CodexMask.ToString());
            Check("reset clears mastery", game.LearnedMask == 0, game.LearnedMask.ToString());
            Check("reset clears run mask", game.RunEchoMask == 0, game.RunEchoMask.ToString());
            Check("reset clears resonance", game.ConductorResonance == 0, game.ConductorResonance.ToString());
            ClearAll();
        }

        private void TestTier3Death()
        {
            Call("StartRun");
            SetField("Attempt", 2);
            SetupPlaying();
            OMTKEnemy killer = AddEnemy(EnemyVariant.Charger, 123f);
            killer.PlaceAtDistance(2.35f);
            Vector3 fatalPosition = killer.Position;
            Call("StartDeath", killer);
            Check("death starts", game.State == GameState.Death, game.State.ToString());
            Check("death tracks fatal azimuth", Near(GetFloat("fatalAzimuth"), 123f), GetFloat("fatalAzimuth").ToString("F1"));
            Check("death tracks fatal clip", GetField("fatalClip") != null);
            Check("death tracks fatal position", Vector3.Distance((Vector3)GetField("fatalPosition"), fatalPosition) < 0.01f);
            Check("death defers resolution", !GetBool("deathResolved"));
            Check("death offers bargain", GetBool("bargainOffered"));
            Check("death bargain eligible", System.Convert.ToInt32(GetField("offeredBargain")) >= 0);

            SetField("StateTime", OMTKConfig.DeathNoteTime + 0.01f);
            Call("UpdateDeath");
            SetField("StateTime", OMTKConfig.DeathCueFirstTime + 0.01f);
            Call("UpdateDeath");
            AudioSource cue = FindLowPassCue();
            Check("death replay has spatial cue", cue != null);
            if (cue != null)
            {
                Check("death replay pitch", Near(cue.pitch, OMTKConfig.DeathCuePitch), cue.pitch.ToString("F2"));
                AudioLowPassFilter filter = cue.GetComponent<AudioLowPassFilter>();
                Check("death replay low pass", filter != null && Near(filter.cutoffFrequency, OMTKConfig.DeathCueLowPass, 1f), filter == null ? "missing" : filter.cutoffFrequency.ToString("F0"));
                Check("death replay at fatal position", Vector3.Distance(cue.transform.position, fatalPosition) < 0.1f, Vector3.Distance(cue.transform.position, fatalPosition).ToString("F2"));
            }

            bool accepted = System.Convert.ToBoolean(Call("TryAcceptBargainBySwipe"));
            Check("bargain accepts", accepted);
            Check("bargain updates run mask", (game.RunEchoMask & OMTKConfig.EchoBit(EchoId.Foresight)) != 0, game.RunEchoMask.ToString());
            Check("bargain updates resonance", game.ConductorResonance == 1, game.ConductorResonance.ToString());
            Check("bargain does not bypass rehearsal", !GetBool("deathResolved"));

            SetYaw(GetFloat("fatalAzimuth"));
            SetField("StateTime", OMTKConfig.DeathCueFirstTime + 0.01f);
            Call("UpdateDeath");
            float windowStart = GetFloat("deathWindowStart");
            Check("death cue opens window", windowStart > 0f, windowStart.ToString("F2"));
            SetField("StateTime", windowStart + 0.01f);
            SetYaw(GetFloat("fatalAzimuth"));
            Call("HandleTap");
            Check("death rehearsal succeeds", GetBool("deathResolved"));
            Check("death success records codex", (game.CodexMask & OMTKConfig.CodexBit(EnemyVariant.Charger)) != 0, game.CodexMask.ToString());
            Check("death retry bounded", GetFloat("deathRetryAt") <= 5f, GetFloat("deathRetryAt").ToString("F2"));
            Check("rehearsal bonus active", game.RehearsalBonusActive);
            Call("Retry");
            Check("rehearsal bonus expires next attempt", !game.RehearsalBonusActive);

            Call("StartRun");
            SetField("Attempt", 2);
            SetupPlaying();
            killer = AddEnemy(EnemyVariant.Stalker, 42f);
            killer.PlaceAtDistance(2.35f);
            Call("StartDeath", killer);
            SetField("StateTime", OMTKConfig.DeathFallbackDuration + 0.01f);
            Call("UpdateDeath");
            Check("death fallback resolves", GetBool("deathResolved"));
            Check("death fallback bounded", GetFloat("deathRetryAt") <= 5f, GetFloat("deathRetryAt").ToString("F2"));

            Call("StartRun");
            SetField("Attempt", 2);
            SetupPlaying();
            killer = AddEnemy(EnemyVariant.Conjurer, -64f);
            killer.PlaceAtDistance(2.35f);
            Call("StartDeath", killer);
            SetField("bargainOffered", false);
            SetYaw(GetFloat("fatalAzimuth"));
            SetField("StateTime", 1f);
            Call("HandleTap");
            Check("death tap before cue does not resolve", !GetBool("deathResolved"));
            SetField("StateTime", OMTKConfig.DeathCueFirstTime + 0.01f);
            SetYaw(0f);
            Call("UpdateDeath");
            float failWindowStart = GetFloat("deathWindowStart");
            SetField("StateTime", failWindowStart + 0.01f);
            SetYaw(GetFloat("fatalAzimuth") + 50f);
            Call("HandleTap");
            Check("death misaligned rehearsal does not resolve", !GetBool("deathResolved"));
            SetField("StateTime", failWindowStart + OMTKConfig.DeathCueWindowDuration + 0.01f);
            SetYaw(GetFloat("fatalAzimuth"));
            Call("HandleTap");
            Check("death mistimed rehearsal does not resolve", !GetBool("deathResolved"));
            SetField("StateTime", failWindowStart + 0.01f);
            SetYaw(GetFloat("fatalAzimuth"));
            Call("UpdateDeath");
            Call("HandleTap");
            Check("death rehearsal recovers after failure", GetBool("deathResolved"));

            Call("StartRun");
            SetField("Attempt", 2);
            SetupPlaying();
            killer = AddEnemy(EnemyVariant.Charger, 77f);
            killer.PlaceAtDistance(2.35f);
            Call("StartDeath", killer);
            SetField("StateTime", OMTKConfig.DeathCueFirstTime + 0.01f);
            SetYaw(0f);
            Call("UpdateDeath");
            SetField("StateTime", GetFloat("deathWindowStart") + 0.01f);
            SetYaw(GetFloat("fatalAzimuth"));
            Call("UpdateDeath");
            Call("HandleTap");
            Check("aligned rehearsal rejects bargain", GetBool("deathResolved") && GetBool("bargainRejected") && !GetBool("bargainOffered"));
            Check("rehearsal rejection leaves run unpowered", game.RunEchoMask == 0 && game.ConductorResonance == 0, game.RunEchoMask + "/" + game.ConductorResonance);

            Call("StartRun");
            SetField("Attempt", 2);
            SetupPlaying();
            killer = AddEnemy(EnemyVariant.Stalker, 30f);
            killer.PlaceAtDistance(2.35f);
            Call("StartDeath", killer);
            SetField("StateTime", 0.5f);
            Call("SkipDeathRehearsal");
            Check("death skip returns control", GetBool("deathResolved") && GetFloat("deathRetryAt") <= 5f, GetFloat("deathRetryAt").ToString("F2"));
            ClearAll();
        }

        private IEnumerator TestTier3Finale()
        {
            Call("StartRun");
            SetField("Attempt", 2);
            SetField("ConductorResonance", 0);
            Call("StartFinale");
            SetField("StateTime", OMTKConfig.FinaleDriftStart + OMTKConfig.FinaleDriftDuration + 0.01f);
            Call("UpdateFinale");
            SetField("Hp", 2);
            SetField("StateTime", GetFloat("finaleThreatDeadline") + 0.01f);
            Call("UpdateFinale");
            Check("finale timeout costs one hp", game.Hp == 1, game.Hp.ToString());

            Call("StartRun");
            SetField("Attempt", 2);
            SetField("RunEchoMask", OMTKConfig.EchoBit(EchoId.Grace) | OMTKConfig.EchoBit(EchoId.Iron));
            SetField("ConductorResonance", 2);
            SetField("CodexMask", 3);
            SetField("LearnedMask", 1);
            Call("StartFinale");
            Check("finale starts beat one", game.State == GameState.Finale && System.Convert.ToInt32(GetField("finaleBeat")) == 1);
            SetField("StateTime", OMTKConfig.FinaleDriftStart + 0.05f);
            Call("UpdateFinale");
            Check("conductor starts in front", Near(game.Audio.ConductorBearing, 0f, 5f), game.Audio.ConductorBearing.ToString("F1"));
            SetField("StateTime", OMTKConfig.FinaleDriftStart + OMTKConfig.FinaleDriftDuration * 0.5f);
            Call("UpdateFinale");
            Check("conductor drift midpoint", Near(game.Audio.ConductorBearing, 90f, 10f), game.Audio.ConductorBearing.ToString("F1"));
            SetField("StateTime", OMTKConfig.FinaleDriftStart + OMTKConfig.FinaleDriftDuration + 0.01f);
            Call("UpdateFinale");
            Check("finale enters harvest", System.Convert.ToInt32(GetField("finaleBeat")) == 2);
            Check("harvest uses resonance", System.Convert.ToInt32(GetField("finaleThreatsRemaining")) == 3, GetField("finaleThreatsRemaining").ToString());
            Check("first true threat behind", Near(GetFloat("finaleTrueBearing"), 180f), GetFloat("finaleTrueBearing").ToString("F1"));
            float trueBeforePrompt = GetFloat("finaleTrueBearing");
            Call("DeceptiveFinalePrompt");
            Check("false prompt does not move true bearing", Near(GetFloat("finaleTrueBearing"), trueBeforePrompt));

            int hp = game.Hp;
            SetYaw(GetFloat("finaleDecoyBearing"));
            SetField("StateTime", GetFloat("finaleThreatWindowStart") + 0.01f);
            Call("TryFinaleSwipe");
            Check("decoy strike costs one hp", game.Hp == hp - 1, game.Hp + "/" + hp);

            SetField("StateTime", GetFloat("finaleNextThreatAt") + 0.01f);
            Call("UpdateFinale");
            hp = game.Hp;
            SetYaw(0f);
            SetField("StateTime", GetFloat("finaleThreatWindowStart") + 0.01f);
            Call("TryFinaleParry");
            Check("off-axis finale parry costs one hp", game.Hp == hp - 1, game.Hp + "/" + hp);

            SetField("StateTime", GetFloat("finaleNextThreatAt") + 0.01f);
            Call("UpdateFinale");
            SetField("Hp", 1);
            SetField("StateTime", GetFloat("finaleThreatDeadline") + 0.01f);
            Call("UpdateFinale");
            Check("finale zero hp restarts finale", game.State == GameState.Finale && game.Hp == 1, game.State + "/" + game.Hp);
            Check("finale restart returns to beat one", System.Convert.ToInt32(GetField("finaleBeat")) == 1, GetField("finaleBeat").ToString());

            SetField("Hp", 3);
            SetField("StateTime", 0f);
            Call("BeginFinaleClimax");
            SetField("StateTime", GetFloat("finaleFinalWindowStart") + 0.01f);
            SetYaw(0f);
            Call("TryFinaleParry");
            Check("wrong final defense costs one hp", !GetBool("finaleFinalDefended") && game.Hp == 2, game.Hp.ToString());
            SetField("StateTime", GetFloat("finaleFinalWindowStart") + 0.01f);
            SetYaw(180f);
            Call("TryFinaleParry");
            Check("final defense accepts aligned tap", GetBool("finaleFinalDefended"));
            SetYaw(0f);
            Call("TryFinaleSwipe");
            Check("wrong final swipe does not win", !GetBool("finaleEnding") && game.Hp == 1, game.Hp.ToString());
            SetField("StateTime", GetFloat("finaleFinalWindowStart") + 0.01f);
            SetYaw(180f);
            Call("TryFinaleParry");
            Check("final defense recovers after wrong swipe", GetBool("finaleFinalDefended"));
            Call("TryFinaleSwipe");
            Check("finale victory sequence begins", GetBool("finaleEnding"));
            yield return new WaitForSecondsRealtime(2.2f);
            Check("finale reaches victory", game.State == GameState.Victory, game.State.ToString());
            Check("victory clears run mask", game.RunEchoMask == 0, game.RunEchoMask.ToString());
            Check("victory clears resonance", game.ConductorResonance == 0, game.ConductorResonance.ToString());
            Check("victory silences tension", !game.Audio.TensionActive, game.Audio.TensionActive.ToString());
            Check("victory preserves codex", (game.CodexMask & 3) == 3, game.CodexMask.ToString());
            Check("sheathe clip exists", game.Audio.SheatheClip != null);
            ClearAll();
        }

        private IList GetEnemies()
        {
            return enemiesField == null ? null : enemiesField.GetValue(game) as IList;
        }

        private bool PairSeparationAtLeast(IList list, float minimum)
        {
            for (int i = 0; i < list.Count; i++)
            {
                OMTKEnemy first = list[i] as OMTKEnemy;
                for (int j = i + 1; j < list.Count; j++)
                {
                    OMTKEnemy second = list[j] as OMTKEnemy;
                    if (first != null && second != null && Mathf.Abs(Mathf.DeltaAngle(first.Azimuth, second.Azimuth)) < minimum)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private void SetEnemyField(OMTKEnemy enemy, string name, object value)
        {
            FieldInfo field = typeof(OMTKEnemy).GetField(name, Flags);
            if (field == null)
            {
                field = typeof(OMTKEnemy).GetField("<" + name + ">k__BackingField", Flags);
            }

            if (field != null)
            {
                field.SetValue(enemy, value);
            }
        }

        private OMTKEnemy SetupEnemy(float angleOffset, float windup, bool locked)
        {
            SetupPlaying();
            OMTKEnemy enemy = AddEnemy(EnemyVariant.Stalker, 0f);
            enemy.PlaceAtDistance(2.35f);
            enemy.BeginScriptedWindup(windup);
            SetField("primaryToken", enemy);
            SetYaw(enemy.Azimuth + angleOffset);
            SetEnemyLock(enemy, locked);
            return enemy;
        }

        private void SetupPlaying()
        {
            Call("ClearEnemies");
            Call("ClearTutorial");
            SetField("primaryToken", null);
            SetField("guardLockout", 0f);
            SetField("Attempt", 2);
            SetField("Wave", 1);
            SetField("Hp", 3);
            Call("SetState", GameState.Playing);
        }

        private void ClearAll()
        {
            Call("ClearEnemies");
            Call("ClearTutorial");
            Call("ClearDeathRehearsal");
            SetField("fatalClip", null);
            SetField("primaryToken", null);
            SetField("offbeatToken", null);
            SetField("pendingToken", null);
            SetField("offbeatDelay", 0f);
            SetField("guardLockout", 0f);
            game.Audio.StopTinnitus();
            Call("SetState", GameState.Title);
        }

        private OMTKEnemy AddEnemy(EnemyVariant variant, float azimuth)
        {
            Call("Spawn", variant, azimuth);
            IList list = enemiesField == null ? null : enemiesField.GetValue(game) as IList;
            return list == null || list.Count == 0 ? null : list[list.Count - 1] as OMTKEnemy;
        }

        private void SetEnemyLock(OMTKEnemy enemy, bool locked)
        {
            FieldInfo field = typeof(OMTKEnemy).GetField("<IsResonanceLocked>k__BackingField", Flags);
            if (field != null)
            {
                field.SetValue(enemy, locked);
            }
        }

        private float GetEnemyFloat(OMTKEnemy enemy, string name)
        {
            FieldInfo field = typeof(OMTKEnemy).GetField(name, Flags);
            return field == null ? 0f : (float)field.GetValue(enemy);
        }

        private float GetMusicCutoff()
        {
            FieldInfo field = typeof(OMTKAudio).GetField("musicFilter", Flags);
            AudioLowPassFilter filter = field == null ? null : field.GetValue(game.Audio) as AudioLowPassFilter;
            return filter == null ? 0f : filter.cutoffFrequency;
        }

        private FieldInfo FindField(string name)
        {
            FieldInfo field = typeof(OMTKGame).GetField(name, Flags);
            if (field == null)
            {
                field = typeof(OMTKGame).GetField("<" + name + ">k__BackingField", Flags);
            }

            return field;
        }

        private object GetField(string name)
        {
            FieldInfo field = FindField(name);
            return field == null ? null : field.GetValue(game);
        }

        private void SetField(string name, object value)
        {
            FieldInfo field = FindField(name);
            if (field != null)
            {
                field.SetValue(game, value);
            }
        }

        private bool GetBool(string name)
        {
            object value = GetField(name);
            return value is bool && (bool)value;
        }

        private float GetFloat(string name)
        {
            object value = GetField(name);
            return value is float ? (float)value : 0f;
        }

        private object Call(string name, params object[] args)
        {
            MethodInfo method = typeof(OMTKGame).GetMethod(name, Flags);
            if (method == null)
            {
                Debug.LogError("[SUITE] missing method " + name);
                return null;
            }

            return method.Invoke(game, args);
        }

        private void Check(string name, bool condition, string detail = "")
        {
            if (condition)
            {
                suitePassed++;
                Debug.Log("[TEST] PASS " + name);
            }
            else
            {
                suiteFailed++;
                Debug.LogError("[TEST] FAIL " + name + (string.IsNullOrEmpty(detail) ? string.Empty : " detail=" + detail));
            }
        }

        private static AudioSource FindLowPassCue()
        {
            AudioSource[] sources = Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
            for (int i = 0; i < sources.Length; i++)
            {
                if (sources[i] != null && sources[i].gameObject.name == "SpatialCue" && sources[i].GetComponent<AudioLowPassFilter>() != null)
                {
                    return sources[i];
                }
            }

            return null;
        }

        private static float MagnitudeAt(AudioClip clip, float frequency)
        {
            if (clip == null || clip.samples <= 0 || clip.channels <= 0)
            {
                return 0f;
            }

            float[] samples = new float[clip.samples * clip.channels];
            if (!clip.GetData(samples, 0))
            {
                return 0f;
            }

            double real = 0.0;
            double imaginary = 0.0;
            int count = 0;
            for (int i = 0; i < clip.samples; i++)
            {
                double phase = 2.0 * System.Math.PI * frequency * i / clip.frequency;
                float sample = samples[i * clip.channels];
                real += sample * System.Math.Cos(phase);
                imaginary += sample * System.Math.Sin(phase);
                count++;
            }

            if (count == 0)
            {
                return 0f;
            }

            return (float)(System.Math.Sqrt(real * real + imaginary * imaginary) / count);
        }

        private static bool Near(float a, float b, float tolerance = 0.01f)
        {
            return Mathf.Abs(a - b) <= tolerance;
        }

        private void UpdatePlaying()
        {
            OMTKEnemy token = GetToken();
            if (token != lastToken)
            {
                lastToken = token;
                if (token != null)
                {
                    Debug.Log("[PLAYTEST] token=" + token.Variant + " windup=" + token.WindupRemaining.ToString("F2") + " locked=" + token.IsResonanceLocked);
                }
            }

            if (token != null)
            {
                SetYaw(token.Azimuth);
                if (token.IsWindingUp && token.WindupRemaining <= game.PerfectWindow)
                {
                    Tap("perfect@" + token.WindupRemaining.ToString("F3") + " locked=" + token.IsResonanceLocked);
                }

                return;
            }

            OMTKEnemy staggered = FindStaggered();
            if (staggered != null)
            {
                SetYaw(staggered.Azimuth);
                Swipe();
                Debug.Log("[PLAYTEST] slash staggered " + staggered.Variant);
            }
        }

        private void UpdateFinale()
        {
            int beat = finaleBeatField == null ? 0 : (int)finaleBeatField.GetValue(game);
            float trueBearing = finaleTrueBearingField == null ? 180f : (float)finaleTrueBearingField.GetValue(game);
            SetYaw(trueBearing);
            if (beat == 3)
            {
                bool defended = finaleFinalDefendedField != null && (bool)finaleFinalDefendedField.GetValue(game);
                if (defended)
                {
                    if (!finaleSwiped)
                    {
                        finaleSwiped = true;
                        Swipe();
                        Debug.Log("[PLAYTEST] finale swipe");
                    }

                    return;
                }

                float start = finaleFinalWindowStartField == null ? 999f : (float)finaleFinalWindowStartField.GetValue(game);
                float end = finaleFinalWindowEndField == null ? -1f : (float)finaleFinalWindowEndField.GetValue(game);
                if (game.StateTime >= start && game.StateTime <= end)
                {
                    Tap("finale final defense");
                }

                return;
            }

            if (beat != 2)
            {
                return;
            }

            bool active = finaleThreatActiveField != null && (bool)finaleThreatActiveField.GetValue(game);
            bool resolved = finaleThreatResolvedField != null && (bool)finaleThreatResolvedField.GetValue(game);
            if (!active || resolved)
            {
                return;
            }

            float windowStart = finaleThreatWindowStartField == null ? 999f : (float)finaleThreatWindowStartField.GetValue(game);
            float windowEnd = finaleThreatWindowEndField == null ? -1f : (float)finaleThreatWindowEndField.GetValue(game);
            if (game.StateTime >= windowStart && game.StateTime <= windowEnd)
            {
                Tap("finale threat");
            }
        }

        private void Tap(string reason = "")
        {
            Call("HandleTap");
            if (!string.IsNullOrEmpty(reason))
            {
                Debug.Log("[PLAYTEST] tap " + reason);
            }
        }

        private void Swipe()
        {
            Call("HandleSwipe");
        }

        private void SetYaw(float yaw)
        {
            if (playerYawField != null)
            {
                playerYawField.SetValue(game, Mathf.Repeat(yaw, 360f));
            }
        }

        private OMTKEnemy GetToken()
        {
            OMTKEnemy primary = tokenField == null ? null : tokenField.GetValue(game) as OMTKEnemy;
            if (primary != null)
            {
                return primary;
            }

            return offbeatTokenField == null ? null : offbeatTokenField.GetValue(game) as OMTKEnemy;
        }

        private OMTKEnemy FindStaggered()
        {
            IList list = enemiesField == null ? null : enemiesField.GetValue(game) as IList;
            if (list == null)
            {
                return null;
            }

            for (int i = 0; i < list.Count; i++)
            {
                OMTKEnemy enemy = list[i] as OMTKEnemy;
                if (enemy != null && enemy.IsStaggered)
                {
                    return enemy;
                }
            }

            return null;
        }
    }
}
