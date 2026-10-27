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
        private FieldInfo enemiesField;
        private FieldInfo finaleFacingField;
        private FieldInfo playerYawField;
        private GameState lastState = (GameState)(-1);
        private OMTKEnemy lastToken;
        private bool finaleSwiped;
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

            tokenField = typeof(OMTKGame).GetField("token", Flags);
            enemiesField = typeof(OMTKGame).GetField("enemies", Flags);
            finaleFacingField = typeof(OMTKGame).GetField("finaleFacing", Flags);
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
                    if (game.StateTime > 1.2f)
                    {
                        Tap("retry");
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
            TestResonanceLock();
            yield return TestPrematureAndRecovery();
            TestShoveAndSlash();
            TestRadar();
            TestCalibration();
            TestTutorialDeath();
            TestProgress();
            TestVolumeAndGestures();

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
            SetField("EchoMask", 0);
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
            SetField("token", null);
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
            SetField("token", enemy);
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
            SetField("token", enemy);
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
            Check("echo ring stays non-directional", echoRing != null && echoRing.gameObject.activeSelf);

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
            Check("tutorial windup starts", GetBool("tutorialWindupStarted") && killer != null && GetField("token") == killer);
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

        private OMTKEnemy SetupEnemy(float angleOffset, float windup, bool locked)
        {
            SetupPlaying();
            OMTKEnemy enemy = AddEnemy(EnemyVariant.Stalker, 0f);
            enemy.PlaceAtDistance(2.35f);
            enemy.BeginScriptedWindup(windup);
            SetField("token", enemy);
            SetYaw(enemy.Azimuth + angleOffset);
            SetEnemyLock(enemy, locked);
            return enemy;
        }

        private void SetupPlaying()
        {
            Call("ClearEnemies");
            Call("ClearTutorial");
            SetField("token", null);
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
            SetField("token", null);
            SetField("guardLockout", 0f);
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

        private object GetField(string name)
        {
            FieldInfo field = typeof(OMTKGame).GetField(name, Flags);
            return field == null ? null : field.GetValue(game);
        }

        private void SetField(string name, object value)
        {
            FieldInfo field = typeof(OMTKGame).GetField(name, Flags);
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
            if (playerYawField != null)
            {
                playerYawField.SetValue(game, 180f);
            }

            bool facing = finaleFacingField != null && (bool)finaleFacingField.GetValue(game);
            if (facing && !finaleSwiped)
            {
                finaleSwiped = true;
                Swipe();
                Debug.Log("[PLAYTEST] finale swipe");
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
            return tokenField == null ? null : tokenField.GetValue(game) as OMTKEnemy;
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
