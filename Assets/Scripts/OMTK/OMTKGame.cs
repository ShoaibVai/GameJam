using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Gyroscope = UnityEngine.InputSystem.Gyroscope;

namespace OMTK
{
    public enum GameState
    {
        Title,
        Intro,
        Calibration,
        Playing,
        Interlude,
        Death,
        Finale,
        Victory
    }

    public class OMTKGame : MonoBehaviour
    {
        public GameState State { get; private set; }
        public int Hp { get; private set; }
        public int MaxHp { get; private set; }
        public int Wave { get; private set; }
        public int Attempt { get; private set; }
        public int Deaths { get; private set; }
        public int BestWave { get; private set; }
        public int EchoMask { get; private set; }
        public float PlayerYaw { get; private set; }
        public float StateTime { get; private set; }
        public OMTKAudio Audio { get; private set; }

        public float WindupScale
        {
            get { return HasEcho(EchoId.Stillness) ? 1.16f : 1f; }
        }

        public float PerfectWindow
        {
            get { return HasEcho(EchoId.Grace) ? OMTKConfig.GracePerfectWindow : OMTKConfig.PerfectWindow; }
        }

        public float PerfectParryAngle
        {
            get { return HasEcho(EchoId.Grace) ? OMTKConfig.GracePerfectParryAngle : OMTKConfig.PerfectParryAngle; }
        }

        public float GuardLockoutRemaining
        {
            get { return guardLockout; }
        }

        private readonly List<OMTKEnemy> enemies = new List<OMTKEnemy>();
        private readonly float[] audioSamples = new float[256];
        private Camera mainCamera;
        private Image ring;
        private Image radarDial;
        private Text radarReadout;
        private Text[] radarLabels;
        private RectTransform[] radarBlipRoots;
        private Image[] radarBlipCores;
        private Image[] radarBlipGlows;
        private float radarPingTimer;
        private bool radarVisible = true;
        private Text statusText;
        private Text titleText;
        private Text subtitleText;
        private Text promptText;
        private Font uiFont;
        private OMTKEnemy token;
        private float windupCooldown;
        private float guardLockout;
        private float introDuration;
        private float interludeDuration;
        private float subtitleUntil;
        private float parryFlash;
        private float damageFlash;
        private float ringPulse;
        private float finaleNudge;
        private bool finaleFacing;
        private bool echoAnnounced;
        private bool gyroEnabled;
        private bool pointerHeld;
        private bool pointerSwipeDetected;
        private Vector2 pointerStart;
        private Vector2 pointerLast;
        private float pointerDownTime;
        private float pointerTravel;
        private bool applicationPaused;
        private float resumeTimeScale = 1f;
        private AudioClip fatalClip;
        private Vector3 fatalPosition;
        private bool fatalReplayPlayed;
        private OMTKSystemTts systemTts;
        private int narratorMode;
        private bool hapticsEnabled;
        private bool optionsOpen;
        private bool twoFingerActive;
        private int optionsIndex;
        private float twoFingerDownAt;
        private float lastTwoFingerTapAt = -10f;
        private bool twoFingerPending;
        private float twoFingerPendingUntil;
        private float masterVolume = 1f;
        private AudioClip lastCueClip;
        private Vector3 lastCuePosition;
        private float lastCueVolume = 1f;
        private float lastCuePitch = 1f;
        private bool lastCueLowPass;
        private bool calibrationDone;
        private bool calibrationAlignedAnnounced;
        private float calibrationNextNudge;
        private OMTKEnemy tutorialKiller;
        private bool tutorialDeathActive;
        private bool tutorialWindupStarted;
        private float tutorialDeathTimer;
        private bool currentDeathTutorial;

        private const string DeathsKey = "OMTK_Deaths";
        private const string BestWaveKey = "OMTK_BestWave";
        private const string EchoKey = "OMTK_EchoMask";
        private const string NarratorKey = "OMTK_NarratorMode";
        private const string HapticsKey = "OMTK_Haptics";
        private const string RadarAssistKey = "OMTK_RadarAssist";
        private const string VolumeKey = "OMTK_Volume";
        private const int OptionsCount = 4;
        private const float VolumeStep = 0.25f;
        private const float TwoFingerTapWindow = 0.34f;
        private const float TwoFingerMaxTapDuration = 0.55f;
        private const string BootLine = "One More Time, Knight. Tap once to begin. Hold to open options. Turn your head to listen. Tap to parry. Swipe to answer.";
        private const int RadarBlipCount = 4;

        private void Awake()
        {
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Screen.orientation = ScreenOrientation.Portrait;
            LoadProgress();
            LoadAccessibility();
            CreateCamera();
            Audio = new OMTKAudio();
            Audio.Init(gameObject);
            systemTts = new OMTKSystemTts();
            systemTts.Init();
            Audio.SetSystemTts(systemTts);
            Audio.SetNarratorMode(narratorMode);
            CreateUI();
            SetState(GameState.Title);
        }

        private void Start()
        {
            ShowSubtitle("Turn your head.\nTap to parry. Swipe to answer.\nHold for options.", 999f);
            promptText.text = "TAP TO BEGIN";
            StartCoroutine(AnnounceTitle());
        }

        private void OnApplicationPause(bool paused)
        {
            if (Application.isEditor || applicationPaused == paused)
            {
                return;
            }

            applicationPaused = paused;
            AudioListener.pause = paused;
            if (paused)
            {
                Audio.StopVoice();
                resumeTimeScale = Time.timeScale;
                Time.timeScale = 0f;
            }
            else
            {
                Time.timeScale = resumeTimeScale;
            }
        }

        private void OnDestroy()
        {
            if (systemTts != null)
            {
                systemTts.Shutdown();
            }
        }

        private void Update()
        {
            float deltaTime = Mathf.Min(Time.deltaTime, 0.05f);
            if (systemTts != null)
            {
                systemTts.Tick();
            }

            Audio.Tick();
            StateTime += deltaTime;
            guardLockout = Mathf.Max(0f, guardLockout - deltaTime);
            UpdateInput(deltaTime);

            if (twoFingerPending && Time.unscaledTime >= twoFingerPendingUntil)
            {
                twoFingerPending = false;
                if (!(State == GameState.Title && optionsOpen))
                {
                    RepeatLastCueAndHeading();
                }
            }

            if (State == GameState.Intro && StateTime >= introDuration)
            {
                if (Attempt == 1 && !calibrationDone)
                {
                    StartCalibration();
                }
                else
                {
                    BeginWave(1);
                }
            }
            else if (State == GameState.Calibration)
            {
                UpdateCalibration();
            }
            else if (State == GameState.Playing)
            {
                UpdatePlaying(deltaTime);
            }
            else if (State == GameState.Interlude && StateTime >= interludeDuration)
            {
                BeginWave(Wave + 1);
            }
            else if (State == GameState.Death)
            {
                UpdateDeath();
            }
            else if (State == GameState.Finale)
            {
                UpdateFinale();
            }

            UpdateCamera();
            UpdateVisuals(deltaTime);
            UpdateRadar(deltaTime);
            UpdateHud();
        }

        public bool HasEcho(EchoId id)
        {
            return (EchoMask & OMTKConfig.EchoBit(id)) != 0;
        }

        public void EnemyAttack(OMTKEnemy enemy)
        {
            if (State != GameState.Playing)
            {
                return;
            }

            token = null;
            windupCooldown = narratorMode == 0 ? 1.7f : 1.15f;
            if (tutorialDeathActive && enemy == tutorialKiller)
            {
                Hp = 1;
            }

            Hp--;
            damageFlash = 1f;
            PulseHaptic();
            Audio.Play2D(Audio.HitClip, 0.95f, Random.Range(0.92f, 1.04f));
            ShowSubtitle("It found you.", 1.05f);
            promptText.text = "LISTEN";
            if (Hp > 0)
            {
                Audio.Speak("Hit. " + Hp + (Hp == 1 ? " heart." : " hearts."));
            }

            if (Hp <= 0)
            {
                StartDeath(enemy);
            }
        }

        private void UpdatePlaying(float deltaTime)
        {
            if (token != null && (!token.IsAlive || !token.IsWindingUp))
            {
                token = null;
            }

            for (int i = 0; i < enemies.Count; i++)
            {
                enemies[i].Tick(deltaTime, PlayerYaw, PerfectWindow);
            }

            enemies.RemoveAll(enemy => !enemy.IsAlive);

            UpdateTutorialDeath(deltaTime);

            if (enemies.Count == 0)
            {
                if (Wave >= 3)
                {
                    StartFinale();
                }
                else
                {
                    StartInterlude();
                }

                return;
            }

            if (token == null)
            {
                windupCooldown -= deltaTime;
                if (windupCooldown <= 0f)
                {
                    OMTKEnemy chosen = null;
                    for (int i = 0; i < enemies.Count; i++)
                    {
                        if (enemies[i].AtTarget)
                        {
                            chosen = enemies[i];
                            break;
                        }
                    }

                    if (chosen != null && chosen.BeginWindup())
                    {
                        token = chosen;
                        RememberLastCue(token.WindupClip, token.Position, 0.9f, 1f, false);
                    }
                }
            }
        }

        private void StartRun()
        {
            optionsOpen = false;
            Attempt = 1;
            Wave = 1;
            Hp = MaxHp;
            guardLockout = 0f;
            calibrationDone = false;
            calibrationAlignedAnnounced = false;
            ClearTutorial();
            ClearEnemies();
            Audio.StopTinnitus();
            Audio.StopCalibration();
            Audio.Unduck();
            Audio.PlayMusic();
            Audio.StopConductor();
            RecenterYaw();
            introDuration = OMTKConfig.RunIntroDuration;
            SetState(GameState.Intro);
            ShowSubtitle("Headphones on. Turn your head. Tap to parry. Swipe to answer.", 3.4f);
            promptText.text = "LISTEN";
            Audio.Speak("Headphones on. Turn your head. Tap to parry. Swipe to answer.");
        }

        private void Retry()
        {
            Attempt++;
            Wave = 1;
            Hp = MaxHp;
            guardLockout = 0f;
            ClearTutorial();
            ClearEnemies();
            Audio.StopTinnitus();
            Audio.StopCalibration();
            Audio.Unduck();
            Audio.PlayMusic();
            Audio.StopConductor();
            RecenterYaw();
            introDuration = 1.65f;
            SetState(GameState.Intro);
            ShowSubtitle("Again. Listen.", 1.6f);
            promptText.text = "LISTEN";
            Audio.Speak("Again.");
        }

        private void StartCalibration()
        {
            SetState(GameState.Calibration);
            RecenterYaw();
            calibrationAlignedAnnounced = false;
            calibrationNextNudge = 5.2f;
            Audio.StartCalibration(OMTKConfig.CalibrationAzimuth, OMTKConfig.CalibrationToneDistance);
            RememberLastCue(Audio.CalibrationClip, Quaternion.Euler(0f, OMTKConfig.CalibrationAzimuth, 0f) * Vector3.forward * OMTKConfig.CalibrationToneDistance, 0.6f, 1f, false);
            Narrator("Dull means behind. Turn until the blade sings, then tap.", 4.2f);
            promptText.text = "TURN";
            titleText.gameObject.SetActive(false);
        }

        private void UpdateCalibration()
        {
            Audio.UpdateCalibration(PlayerYaw, OMTKConfig.CalibrationAzimuth);
            float angle = Mathf.Abs(Mathf.DeltaAngle(PlayerYaw, OMTKConfig.CalibrationAzimuth));
            bool aligned = angle <= OMTKConfig.CalibrationAlignAngle;

            if (aligned)
            {
                promptText.text = "TAP";
                if (!calibrationAlignedAnnounced)
                {
                    calibrationAlignedAnnounced = true;
                    ShowSubtitle("The blade sings. Tap.", 1.2f);
                    Audio.Speak("Tap.");
                }
            }
            else
            {
                promptText.text = "TURN";
                calibrationAlignedAnnounced = false;
            }

            if (StateTime >= calibrationNextNudge && StateTime < OMTKConfig.CalibrationFallbackDuration - 1.2f)
            {
                calibrationNextNudge = StateTime + OMTKConfig.CalibrationNudgeInterval;
                Narrator(aligned ? "Tap when the blade sings." : "Turn toward the tone.", 1.35f);
            }

            if (StateTime >= OMTKConfig.CalibrationFallbackDuration)
            {
                FinishCalibration(false);
            }
        }

        private void FinishCalibration(bool aligned)
        {
            calibrationDone = true;
            Audio.StopCalibration();
            if (aligned)
            {
                Audio.Play2D(Audio.EchoUnlockClip, 0.45f, 1.35f);
                BeginWave(1);
            }
            else
            {
                Audio.Play2D(Audio.TickClip, 0.6f);
                BeginWave(1);
            }
        }

        private void ClearTutorial()
        {
            tutorialKiller = null;
            tutorialDeathActive = false;
            tutorialWindupStarted = false;
            tutorialDeathTimer = 0f;
        }

        private void BeginWave(int wave)
        {
            Wave = wave;
            Hp = Mathf.Min(MaxHp, Hp + (wave > 1 ? 1 : 0));
            guardLockout = 0f;
            ClearEnemies();
            windupCooldown = narratorMode == 0 ? 2.55f : 1.65f;
            SetState(GameState.Playing);
            RecenterYaw();

            if (wave == 1)
            {
                float bearing = Attempt == 1 ? OMTKConfig.CalibrationAzimuth : 62f;
                Spawn(EnemyVariant.Stalker, bearing);
                if (Attempt == 1)
                {
                    tutorialKiller = enemies[enemies.Count - 1];
                    tutorialKiller.PlaceAtDistance(5f);
                    tutorialDeathActive = true;
                    tutorialWindupStarted = false;
                    tutorialDeathTimer = OMTKConfig.TutorialFirstDeathDelay;
                    windupCooldown = 999f;
                }

                Narrator("Wave one. Something is circling you.", 2.25f);
            }
            else if (wave == 2)
            {
                Spawn(EnemyVariant.Charger, -74f);
                Narrator("Wave two. It learns your rhythm.", 2.25f);
            }
            else
            {
                Spawn(EnemyVariant.Conjurer, 142f);
                Spawn(EnemyVariant.Stalker, -38f);
                Narrator("Wave three. Do not trust the silence.", 2.25f);
            }

            promptText.text = "LISTEN";
            titleText.gameObject.SetActive(false);
        }

        private void UpdateTutorialDeath(float deltaTime)
        {
            if (!tutorialDeathActive || tutorialKiller == null)
            {
                return;
            }

            if (!tutorialKiller.IsAlive)
            {
                tutorialDeathActive = false;
                return;
            }

            tutorialDeathTimer -= deltaTime;
            if (!tutorialWindupStarted && tutorialDeathTimer <= 0f)
            {
                tutorialWindupStarted = true;
                if (tutorialKiller.BeginScriptedWindup(OMTKConfig.TutorialFirstDeathWindup))
                {
                    token = tutorialKiller;
                    RememberLastCue(token.WindupClip, token.Position, 0.9f, 1f, false);
                    ShowSubtitle("The first cut is inevitable.", 1.1f);
                    promptText.text = "LISTEN";
                }
            }
        }

        private void StartInterlude()
        {
            SetState(GameState.Interlude);
            interludeDuration = 2.35f;
            Hp = Mathf.Min(MaxHp, Hp + 1);
            damageFlash = 0f;
            Audio.Play2D(Audio.EchoUnlockClip, 0.35f, 1.35f);
            Narrator("The hall exhales. One more time.", 1.85f);
            promptText.text = "BREATHE";
        }

        private void StartFinale()
        {
            ClearEnemies();
            SetState(GameState.Finale);
            Audio.Duck(0.82f);
            Audio.StartConductor(new Vector3(0f, 0f, -4f));
            finaleFacing = false;
            finaleNudge = 0f;
            ShowSubtitle("The Conductor is behind you. Turn.", 2.2f);
            promptText.text = "TURN";
            Audio.Speak("The Conductor is behind you. Turn.");
        }

        private void UpdateFinale()
        {
            Audio.UpdateConductor(PlayerYaw);
            float angle = Mathf.Abs(Mathf.DeltaAngle(PlayerYaw, 180f));

            if (!finaleFacing && angle <= OMTKConfig.FinaleTurnWindow && StateTime >= 0.65f)
            {
                finaleFacing = true;
                Audio.Play2D(Audio.ConductorReadyClip, 0.9f);
                RememberLastCue(Audio.ConductorReadyClip, new Vector3(0f, 0f, -4f), 0.9f, 1f, false);
                Narrator("Now. Swipe.", 1.4f);
                promptText.text = "SWIPE";
            }

            if (!finaleFacing && StateTime - finaleNudge > 4.5f)
            {
                finaleNudge = StateTime;
                Narrator("Turn toward the voice.", 1.6f);
            }
        }

        private void StartDeath(OMTKEnemy killer)
        {
            SetState(GameState.Death);
            Hp = 0;
            Deaths++;
            BestWave = Mathf.Max(BestWave, Wave);
            SaveProgress();
            token = null;
            for (int i = 0; i < enemies.Count; i++)
            {
                enemies[i].Stop();
            }

            echoAnnounced = false;
            guardLockout = 0f;
            damageFlash = 1f;
            PulseHaptic();
            Audio.StopTinnitus();
            currentDeathTutorial = tutorialDeathActive;
            tutorialDeathActive = false;
            tutorialWindupStarted = false;
            tutorialKiller = null;
            fatalClip = killer.WindupClip;
            fatalPosition = killer.Position;
            RememberLastCue(fatalClip, fatalPosition, 0.9f, 0.62f, true);
            fatalReplayPlayed = false;
            Audio.StopVoice();
            Audio.PlayFatal(fatalClip, fatalPosition);
            Audio.Play2D(Audio.HeartbeatClip, 0.9f);
            ShowSubtitle("One more time.", 1.45f);
            promptText.text = string.Empty;
            Audio.Speak("One more time.");
        }

        private void UpdateDeath()
        {
            if (!echoAnnounced && StateTime >= 1.05f)
            {
                echoAnnounced = true;
                if (currentDeathTutorial)
                {
                    promptText.text = "TAP TO RISE";
                }
                else
                {
                    EchoId? unlocked = UnlockNextEcho();
                    if (unlocked.HasValue)
                    {
                        Audio.Play2D(Audio.EchoUnlockClip, 0.85f);
                        Narrator(OMTKConfig.EchoName(unlocked.Value) + ". " + OMTKConfig.EchoHint(unlocked.Value), 2.25f);
                        promptText.text = "TAP TO RISE";
                    }
                    else
                    {
                        promptText.text = "TAP TO RISE";
                    }
                }
            }

            if (!fatalReplayPlayed && StateTime >= 3.45f && fatalClip != null)
            {
                fatalReplayPlayed = true;
                Audio.PlayAt(fatalClip, fatalPosition, 0.85f, 0.62f, true);
                if (currentDeathTutorial)
                {
                    ShowSubtitle("Turn toward the sound.", 1.25f);
                    Audio.Speak("Turn toward the sound.");
                }
                else
                {
                    ShowSubtitle("Remember this.", 1.2f);
                }
            }

            if (StateTime >= 4.8f)
            {
                Retry();
            }
        }

        private void TryParry()
        {
            if (State != GameState.Playing)
            {
                return;
            }

            if (guardLockout > 0f)
            {
                return;
            }

            if (tutorialDeathActive && tutorialWindupStarted && token == tutorialKiller)
            {
                Audio.Play2D(Audio.OffAxisClip, 0.35f);
                ShowSubtitle("This blow cannot be stopped.", 1f);
                return;
            }

            if (token != null && token.IsWindingUp)
            {
                float angle = Mathf.Abs(Mathf.DeltaAngle(PlayerYaw, token.Azimuth));
                if (angle > OMTKConfig.GlancingDeflectAngle)
                {
                    Audio.Play2D(Audio.OffAxisClip, 0.5f);
                    ShowSubtitle("Wide. Face the sound.", 0.9f);
                    promptText.text = "TURN";
                    return;
                }

                if (token.WindupRemaining > OMTKConfig.StandardParryWindow)
                {
                    TriggerPrematureTap();
                    return;
                }

                ParryGrade grade = ParryGrade.Standard;
                if (angle > OMTKConfig.StandardParryAngle)
                {
                    grade = ParryGrade.Glancing;
                }
                else if (angle <= PerfectParryAngle && token.WindupRemaining <= PerfectWindow && token.IsResonanceLocked)
                {
                    grade = ParryGrade.Perfect;
                }

                token.Parry(grade);
                token = null;
                float baseCooldown = narratorMode == 0 ? 1.3f : 0.9f;
                windupCooldown = grade == ParryGrade.Standard
                    ? baseCooldown * OMTKConfig.StandardParryCooldownScale
                    : baseCooldown;

                if (grade == ParryGrade.Perfect)
                {
                    parryFlash = 1f;
                    PulseHaptic();
                    Audio.Play2D(Audio.PerfectClip, 0.9f);
                    ShowSubtitle("Now. Swipe.", 1.15f);
                    promptText.text = "SWIPE";
                    Audio.Speak("Perfect. Swipe.");
                }
                else if (grade == ParryGrade.Glancing)
                {
                    parryFlash = 0.45f;
                    ShowSubtitle("Glancing. It slips away.", 1.05f);
                    promptText.text = "LISTEN";
                    Audio.Speak("Glancing.");
                }
                else
                {
                    Audio.Play2D(Audio.ParryClip, 0.78f);
                    ShowSubtitle("Blocked. Wait for the opening.", 1.2f);
                    promptText.text = "LISTEN";
                    Audio.Speak("Blocked. Listen.");
                }

                return;
            }

            OMTKEnemy staggered = FindStaggered();
            if (staggered != null)
            {
                if (staggered.ShoveAway())
                {
                    Audio.Play2D(Audio.WhiffClip, 0.42f, 0.86f);
                    ShowSubtitle("Shoved back. Swipe to finish.", 0.9f);
                    promptText.text = "SWIPE";
                    windupCooldown = 0.95f;
                }

                return;
            }

            if (HasLiveEnemy())
            {
                TriggerPrematureTap();
                return;
            }

            Audio.Play2D(Audio.WhiffClip, 0.4f);
            ShowSubtitle("Nothing there.", 0.65f);
        }

        private void TriggerPrematureTap()
        {
            if (guardLockout > 0f)
            {
                return;
            }

            guardLockout = OMTKConfig.GuardLockoutDuration;
            PulseHaptic();
            Audio.PlayTinnitus();
            ShowSubtitle("Too soon. Your guard is open.", 1f);
            promptText.text = "WAIT";
        }

        private void TrySlash()
        {
            if (State != GameState.Playing)
            {
                return;
            }

            OMTKEnemy staggered = FindStaggered();
            if (staggered != null)
            {
                KillEnemy(staggered);
                return;
            }

            Audio.Play2D(Audio.WhiffClip, 0.46f);
            ShowSubtitle("The blade finds air.", 0.7f);
        }

        private bool KillEnemy(OMTKEnemy enemy)
        {
            if (enemy == null)
            {
                return false;
            }

            float angle = Mathf.Abs(Mathf.DeltaAngle(PlayerYaw, enemy.Azimuth));
            if (angle > OMTKConfig.StandardParryAngle)
            {
                Audio.Play2D(Audio.OffAxisClip, 0.5f);
                ShowSubtitle("Not aligned. Face the sound.", 0.95f);
                promptText.text = "TURN";
                return false;
            }

            if (!enemy.Kill())
            {
                return false;
            }

            if (token == enemy)
            {
                token = null;
            }

            windupCooldown = 0.82f;
            parryFlash = 1f;
            PulseHaptic();
            Audio.Play2D(Audio.SlashClip, 0.9f, Random.Range(0.96f, 1.06f));
            ShowSubtitle("One less echo.", 0.95f);
            promptText.text = "LISTEN";
            BestWave = Mathf.Max(BestWave, Wave);
            SaveProgress();
            return true;
        }

        private OMTKEnemy FindStaggered()
        {
            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i].IsStaggered)
                {
                    return enemies[i];
                }
            }

            return null;
        }

        private bool HasLiveEnemy()
        {
            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i].IsAlive)
                {
                    return true;
                }
            }

            return false;
        }

        private void Victory()
        {
            SetState(GameState.Victory);
            BestWave = 3;
            SaveProgress();
            Audio.StopConductor();
            Audio.Unduck();
            PulseHaptic();
            Audio.Play2D(Audio.VictoryClip, 0.9f);
            Audio.Play2D(Audio.PerfectClip, 0.55f, 1.1f);
            ShowSubtitle("You remembered. One more time.", 4f);
            titleText.text = "THE ECHO ENDS";
            titleText.gameObject.SetActive(true);
            promptText.text = "TAP TO BEGIN AGAIN";
            Audio.Speak("You remembered.");
        }

        private void Spawn(EnemyVariant variant, float azimuth)
        {
            OMTKEnemy enemy = new OMTKEnemy(this, Audio, variant, azimuth, enemies.Count);
            enemies.Add(enemy);
        }

        private void ClearEnemies()
        {
            for (int i = 0; i < enemies.Count; i++)
            {
                enemies[i].Destroy();
            }

            enemies.Clear();
            token = null;
        }

        private void UpdateInput(float deltaTime)
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                float keyboardTurn = 0f;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
                {
                    keyboardTurn -= 110f;
                }

                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
                {
                    keyboardTurn += 110f;
                }

                if (keyboardTurn != 0f)
                {
                    PlayerYaw += keyboardTurn * deltaTime;
                }

                if (keyboard.spaceKey.wasPressedThisFrame)
                {
                    HandleTap();
                }

                if (keyboard.fKey.wasPressedThisFrame)
                {
                    HandleSwipe();
                }

                if (keyboard.oKey.wasPressedThisFrame && State == GameState.Title)
                {
                    ToggleOptions();
                }

                if (keyboard.escapeKey.wasPressedThisFrame && optionsOpen)
                {
                    ToggleOptions();
                }

                if (keyboard.hKey.wasPressedThisFrame)
                {
                    RepeatLastCueAndHeading();
                }

                if (keyboard.rKey.wasPressedThisFrame)
                {
                    RecenterYawAndReport();
                }
            }

            if (Gyroscope.current != null)
            {
                if (!gyroEnabled)
                {
                    InputSystem.EnableDevice(Gyroscope.current);
                    gyroEnabled = true;
                }

                Vector3 angularVelocity = Gyroscope.current.angularVelocity.ReadValue();
                if (!float.IsNaN(angularVelocity.y) && !float.IsInfinity(angularVelocity.y))
                {
                    PlayerYaw += -angularVelocity.y * Mathf.Rad2Deg * 1.15f * deltaTime;
                }
            }

            if (float.IsNaN(PlayerYaw) || float.IsInfinity(PlayerYaw))
            {
                PlayerYaw = 0f;
            }

            PlayerYaw = Mathf.Repeat(PlayerYaw, 360f);

            Touchscreen touchscreen = Touchscreen.current;
            Mouse mouse = Mouse.current;

            if (touchscreen != null)
            {
                int pressed = 0;
                for (int i = 0; i < touchscreen.touches.Count; i++)
                {
                    if (touchscreen.touches[i].press.isPressed)
                    {
                        pressed++;
                    }
                }

                if (pressed >= 2 && !twoFingerActive)
                {
                    twoFingerActive = true;
                    twoFingerDownAt = Time.unscaledTime;
                }

                if (twoFingerActive)
                {
                    if (pressed == 0)
                    {
                        twoFingerActive = false;
                        if (Time.unscaledTime - twoFingerDownAt <= TwoFingerMaxTapDuration)
                        {
                            if (optionsOpen)
                            {
                                ToggleOptions();
                            }
                            else
                            {
                                float now = Time.unscaledTime;
                                if (now - lastTwoFingerTapAt <= TwoFingerTapWindow)
                                {
                                    lastTwoFingerTapAt = -10f;
                                    twoFingerPending = false;
                                    RecenterYawAndReport();
                                }
                                else
                                {
                                    lastTwoFingerTapAt = now;
                                    twoFingerPending = true;
                                    twoFingerPendingUntil = now + TwoFingerTapWindow;
                                }
                            }
                        }
                    }

                    pointerHeld = false;
                    return;
                }
            }

            Vector2 position = Vector2.zero;
            bool down = false;
            bool up = false;
            bool held = false;

            if (touchscreen != null && (touchscreen.primaryTouch.press.isPressed || touchscreen.primaryTouch.press.wasReleasedThisFrame))
            {
                position = touchscreen.primaryTouch.position.ReadValue();
                down = touchscreen.primaryTouch.press.wasPressedThisFrame;
                up = touchscreen.primaryTouch.press.wasReleasedThisFrame;
                held = touchscreen.primaryTouch.press.isPressed;
            }
            else if (mouse != null)
            {
                position = mouse.position.ReadValue();
                down = mouse.leftButton.wasPressedThisFrame;
                up = mouse.leftButton.wasReleasedThisFrame;
                held = mouse.leftButton.isPressed;
            }

            if (down)
            {
                pointerHeld = true;
                pointerSwipeDetected = false;
                pointerStart = position;
                pointerLast = position;
                pointerDownTime = 0f;
                pointerTravel = 0f;
            }

            if (pointerHeld)
            {
                pointerDownTime += deltaTime;
                float deltaX = position.x - pointerLast.x;
                if (Mathf.Abs(deltaX) > 0.01f)
                {
                    pointerTravel += Mathf.Abs(deltaX);
                    float velocity = Mathf.Abs(deltaX) / Mathf.Max(0.0001f, deltaTime);
                    bool swipeGesture = velocity >= OMTKConfig.SwipeVelocityThreshold
                        || (Mathf.Abs(position.x - pointerStart.x) >= OMTKConfig.SwipeDistanceThreshold && pointerDownTime < 0.48f);
                    if (swipeGesture)
                    {
                        pointerSwipeDetected = true;
                    }

                    bool canTurn = !pointerSwipeDetected && FindStaggered() == null;
                    if (canTurn && Mathf.Abs(position.x - pointerStart.x) > OMTKConfig.TouchDeadZone)
                    {
                        PlayerYaw += deltaX * 0.18f;
                    }
                }

                pointerLast = position;
            }

            if (up && pointerHeld)
            {
                pointerHeld = false;
                pointerSwipeDetected = false;
                Vector2 delta = position - pointerStart;
                if (State == GameState.Title && pointerTravel < 18f && delta.magnitude < 45f && pointerDownTime >= 0.75f)
                {
                    ToggleOptions();
                    return;
                }

                if (pointerTravel < 18f && delta.magnitude < 45f && pointerDownTime < 0.42f)
                {
                    HandleTap();
                }
                else if (Mathf.Abs(delta.x) > OMTKConfig.SwipeDistanceThreshold && Mathf.Abs(delta.y) < OMTKConfig.SwipeDistanceThreshold && pointerDownTime < 0.48f)
                {
                    HandleSwipe();
                }
            }

            if (float.IsNaN(PlayerYaw) || float.IsInfinity(PlayerYaw))
            {
                PlayerYaw = 0f;
            }

            PlayerYaw = Mathf.Repeat(PlayerYaw, 360f);
        }

        private IEnumerator AnnounceTitle()
        {
            float deadline = Time.unscaledTime + 10f;
            while (systemTts != null && !systemTts.IsReady && !systemTts.IsFailed && Time.unscaledTime < deadline)
            {
                yield return null;
            }

            if (State == GameState.Title && !optionsOpen)
            {
                Audio.Speak(BootLine);
            }
        }

        private void LoadAccessibility()
        {
            narratorMode = Mathf.Clamp(PlayerPrefs.GetInt(NarratorKey, 0), 0, 2);
            hapticsEnabled = PlayerPrefs.GetInt(HapticsKey, 1) == 1;
            OMTKConfig.RadarAssist = PlayerPrefs.GetInt(RadarAssistKey, OMTKConfig.RadarAssist ? 1 : 0) == 1;
            masterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, 1f));
            AudioListener.volume = masterVolume;
        }

        private void RecenterYaw()
        {
            PlayerYaw = 0f;
            Audio.Play2D(Audio.RecenterClip, 0.5f);
        }

        private void RecenterYawAndReport()
        {
            RecenterYaw();
            PulseHaptic();
            ShowSubtitle("RECENTERED\nHP " + Hp + "/" + MaxHp + "   WAVE " + Wave, 1.6f);
            Audio.Speak("Recentered. HP " + Hp + " of " + MaxHp + ". Wave " + Wave + ".");
        }

        private void RememberLastCue(AudioClip clip, Vector3 position, float volume, float pitch, bool lowPass)
        {
            lastCueClip = clip;
            lastCuePosition = position;
            lastCueVolume = volume;
            lastCuePitch = pitch;
            lastCueLowPass = lowPass;
        }

        private void RepeatLastCueAndHeading()
        {
            int heading = Mathf.RoundToInt(Mathf.Repeat(PlayerYaw, 360f)) % 360;
            ShowSubtitle("HEADING " + heading.ToString("000") + "°", 1.5f);
            if (lastCueClip == null)
            {
                Audio.Play2D(Audio.TickClip, 0.4f, 1.2f);
                Audio.Speak("Heading " + heading + " degrees.");
                return;
            }

            Audio.PlayAt(lastCueClip, lastCuePosition, lastCueVolume, lastCuePitch, lastCueLowPass);
            StartCoroutine(SpeakHeadingAfterCue(heading, Mathf.Min(1.1f, lastCueClip.length * 0.6f)));
        }

        private IEnumerator SpeakHeadingAfterCue(int heading, float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            Audio.Speak("Heading " + heading + " degrees.");
        }

        private void ToggleOptions()
        {
            if (State != GameState.Title)
            {
                return;
            }

            optionsOpen = !optionsOpen;
            if (optionsOpen)
            {
                OpenOptions();
            }
            else
            {
                CloseOptions();
            }
        }

        private void OpenOptions()
        {
            optionsIndex = 0;
            titleText.text = "OPTIONS";
            titleText.gameObject.SetActive(true);
            subtitleText.text = OptionsText();
            subtitleText.color = new Color(1f, 0.9f, 0.58f, 1f);
            subtitleUntil = 999f;
            promptText.text = "TAP: CHANGE   SWIPE: NEXT   TWO-FINGER TAP: CLOSE";
            statusText.text = "ACCESSIBILITY";
            Audio.Speak(OptionsSpeech());
        }

        private void MoveOptionsRow()
        {
            optionsIndex = (optionsIndex + 1) % OptionsCount;
            subtitleText.text = OptionsText();
            Audio.Speak(OptionsRowSpeech());
        }

        private void AdjustOptionsRow()
        {
            if (optionsIndex == 0)
            {
                CycleNarrator();
            }
            else if (optionsIndex == 1)
            {
                ToggleHaptics();
            }
            else if (optionsIndex == 2)
            {
                ToggleRadar();
            }
            else
            {
                CycleVolume();
            }
        }

        private void CloseOptions()
        {
            titleText.text = "ONE MORE TIME,\nKNIGHT";
            subtitleText.text = "Turn your head.\nTap to parry. Swipe to answer.\nHold for options.";
            subtitleText.color = new Color(1f, 0.9f, 0.58f, 1f);
            subtitleUntil = 999f;
            promptText.text = "TAP TO BEGIN";
            statusText.text = "HEADPHONES ON";
            Audio.Speak("Options closed. Tap to begin.");
        }

        private void CycleNarrator()
        {
            narratorMode = (narratorMode + 1) % 3;
            Audio.SetNarratorMode(narratorMode);
            PlayerPrefs.SetInt(NarratorKey, narratorMode);
            PlayerPrefs.Save();
            subtitleText.text = OptionsText();
            if (narratorMode == 2)
            {
                Audio.Play2D(Audio.TickClip, 0.55f);
            }
            else
            {
                Audio.Speak("Narrator " + NarratorName().ToLowerInvariant() + ".");
            }
        }

        private void ToggleHaptics()
        {
            hapticsEnabled = !hapticsEnabled;
            PlayerPrefs.SetInt(HapticsKey, hapticsEnabled ? 1 : 0);
            PlayerPrefs.Save();
            subtitleText.text = OptionsText();
            PulseHaptic();
            Audio.Speak("Haptics " + (hapticsEnabled ? "on." : "off."));
        }

        private void ToggleRadar()
        {
            OMTKConfig.RadarAssist = !OMTKConfig.RadarAssist;
            PlayerPrefs.SetInt(RadarAssistKey, OMTKConfig.RadarAssist ? 1 : 0);
            PlayerPrefs.Save();
            SetRadarVisible(OMTKConfig.RadarAssist);
            subtitleText.text = OptionsText();
            Audio.Speak("Radar assist " + (OMTKConfig.RadarAssist ? "on." : "off."));
        }

        private void CycleVolume()
        {
            masterVolume = Mathf.Round((masterVolume - VolumeStep) * 100f) / 100f;
            if (masterVolume < -0.001f)
            {
                masterVolume = 1f;
            }

            AudioListener.volume = masterVolume;
            PlayerPrefs.SetFloat(VolumeKey, masterVolume);
            PlayerPrefs.Save();
            if (masterVolume <= 0.01f)
            {
                Audio.StopVoice();
            }

            subtitleText.text = OptionsText();
            Audio.Speak("Volume " + Mathf.RoundToInt(masterVolume * 100f) + " percent.");
            PulseHaptic();
        }

        private string OptionsText()
        {
            string narrator = "NARRATOR: " + NarratorName();
            string haptics = "HAPTICS: " + (hapticsEnabled ? "ON" : "OFF");
            string radar = "RADAR ASSIST: " + (OMTKConfig.RadarAssist ? "ON" : "OFF");
            string volume = "VOLUME: " + Mathf.RoundToInt(masterVolume * 100f) + "%";
            string[] rows = { narrator, haptics, radar, volume };
            for (int i = 0; i < rows.Length; i++)
            {
                rows[i] = (i == optionsIndex ? "> " : "  ") + rows[i];
            }

            return rows[0] + "\n" + rows[1] + "\n" + rows[2] + "\n" + rows[3] + "\nTAP: CHANGE   SWIPE: NEXT";
        }

        private string OptionsSpeech()
        {
            return "Options. " + OptionsRowSpeech() + " Tap to change. Swipe for next option. Hold or two-finger tap to close.";
        }

        private string OptionsRowSpeech()
        {
            if (optionsIndex == 0)
            {
                return "Narrator " + NarratorName().ToLowerInvariant() + ".";
            }

            if (optionsIndex == 1)
            {
                return "Haptics " + (hapticsEnabled ? "on." : "off.");
            }

            if (optionsIndex == 2)
            {
                return "Radar assist " + (OMTKConfig.RadarAssist ? "on." : "off.") + ".";
            }

            return "Volume " + Mathf.RoundToInt(masterVolume * 100f) + " percent.";
        }

        private string NarratorName()
        {
            if (narratorMode == 1)
            {
                return "SYNTH";
            }

            if (narratorMode == 2)
            {
                return "OFF";
            }

            return "SYSTEM";
        }

        private void PulseHaptic()
        {
            if (!hapticsEnabled)
            {
                return;
            }

#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#endif
        }

        private void HandleTap()
        {
            if (State == GameState.Title)
            {
                if (optionsOpen)
                {
                    AdjustOptionsRow();
                    return;
                }

                StartRun();
            }
            else if (State == GameState.Intro && StateTime > 0.55f)
            {
                if (Attempt == 1 && !calibrationDone)
                {
                    StartCalibration();
                }
                else
                {
                    BeginWave(1);
                }
            }
            else if (State == GameState.Calibration)
            {
                float angle = Mathf.Abs(Mathf.DeltaAngle(PlayerYaw, OMTKConfig.CalibrationAzimuth));
                if (angle <= OMTKConfig.CalibrationAlignAngle)
                {
                    FinishCalibration(true);
                }
                else
                {
                    ShowSubtitle("Not yet. Turn toward the tone.", 1.25f);
                    Audio.Speak("Not yet. Turn toward the tone.");
                    promptText.text = "TURN";
                }
            }
            else if (State == GameState.Playing)
            {
                TryParry();
            }
            else if (State == GameState.Interlude && StateTime > 0.9f)
            {
                BeginWave(Wave + 1);
            }
            else if (State == GameState.Death && StateTime > 0.9f)
            {
                Retry();
            }
            else if (State == GameState.Finale)
            {
                TryFinale();
            }
            else if (State == GameState.Victory && StateTime > 0.6f)
            {
                StartRun();
            }
        }

        private void HandleSwipe()
        {
            if (State == GameState.Title)
            {
                if (optionsOpen)
                {
                    MoveOptionsRow();
                    return;
                }

                StartRun();
            }
            else if (State == GameState.Intro && StateTime > 0.55f)
            {
                if (Attempt == 1 && !calibrationDone)
                {
                    StartCalibration();
                }
                else
                {
                    BeginWave(1);
                }
            }
            else if (State == GameState.Calibration)
            {
                return;
            }
            else if (State == GameState.Playing)
            {
                TrySlash();
            }
            else if (State == GameState.Finale)
            {
                TryFinale();
            }
            else if (State == GameState.Victory && StateTime > 0.6f)
            {
                StartRun();
            }
        }

        private void TryFinale()
        {
            if (!finaleFacing)
            {
                Audio.Play2D(Audio.WhiffClip, 0.45f);
                Narrator("Turn toward the voice.", 1.2f);
                promptText.text = "TURN";
                return;
            }

            Victory();
        }

        private void SetState(GameState state)
        {
            State = state;
            StateTime = 0f;
            if (state == GameState.Playing)
            {
                radarPingTimer = narratorMode == 0 ? 2.8f : 1.8f;
            }

            if (titleText == null)
            {
                return;
            }

            bool showTitle = state == GameState.Title || state == GameState.Victory;
            titleText.gameObject.SetActive(showTitle);
            if (state == GameState.Title)
            {
                titleText.text = "ONE MORE TIME,\nKNIGHT";
            }
        }

        private void UpdateCamera()
        {
            if (mainCamera != null)
            {
                mainCamera.transform.rotation = Quaternion.Euler(0f, PlayerYaw, 0f);
            }
        }

        private void UpdateVisuals(float deltaTime)
        {
            AudioListener.GetOutputData(audioSamples, 0);
            float sum = 0f;
            for (int i = 0; i < audioSamples.Length; i++)
            {
                sum += audioSamples[i] * audioSamples[i];
            }

            float rms = Mathf.Sqrt(sum / audioSamples.Length);
            ringPulse = Mathf.Lerp(ringPulse, Mathf.Clamp01(rms * 12f), deltaTime * 10f);
            parryFlash = Mathf.MoveTowards(parryFlash, 0f, deltaTime * 2.6f);
            damageFlash = Mathf.MoveTowards(damageFlash, 0f, deltaTime * 2.1f);

            if (ring != null)
            {
                float scale = 1f + ringPulse * 0.16f + Mathf.Sin(Time.unscaledTime * 1.7f) * 0.012f;
                ring.rectTransform.localScale = new Vector3(scale, scale, 1f);
                Color baseColor = State == GameState.Finale ? new Color(1f, 0.48f, 0.36f, 0.78f) : new Color(0.68f, 0.86f, 0.96f, 0.72f);
                Color color = Color.Lerp(baseColor, Color.cyan, parryFlash * 0.9f);
                color = Color.Lerp(color, new Color(1f, 0.13f, 0.08f, 0.95f), damageFlash);
                ring.color = color;
            }

            if (subtitleText != null)
            {
                if (subtitleUntil > 0f && Time.unscaledTime >= subtitleUntil)
                {
                    subtitleText.text = string.Empty;
                    subtitleUntil = 0f;
                }
                else if (!string.IsNullOrEmpty(subtitleText.text))
                {
                    float remaining = subtitleUntil - Time.unscaledTime;
                    float alpha = Mathf.Clamp01(remaining / 0.28f);
                    subtitleText.color = new Color(1f, 0.9f, 0.58f, alpha);
                }
            }
        }

        private void UpdateRadar(float deltaTime)
        {
            if (radarDial == null)
            {
                return;
            }

            bool wantRadar = OMTKConfig.RadarAssist && State != GameState.Calibration;
            SetRadarVisible(wantRadar);
            if (!wantRadar)
            {
                return;
            }

            bool active = State == GameState.Playing || State == GameState.Finale || State == GameState.Death;
            Color dialColor = State == GameState.Finale
                ? new Color(1f, 0.56f, 0.38f, active ? 0.28f : 0.1f)
                : State == GameState.Death
                    ? new Color(1f, 0.34f, 0.28f, active ? 0.25f : 0.1f)
                    : new Color(0.64f, 0.84f, 1f, active ? 0.24f : 0.1f);
            radarDial.color = dialColor;
            radarDial.rectTransform.localScale = Vector3.one * (1f + ringPulse * 0.015f);

            if (radarReadout != null)
            {
                float heading = Mathf.Repeat(PlayerYaw, 360f);
                radarReadout.text = "HDG " + Mathf.RoundToInt(heading).ToString("000") + "°";
                radarReadout.color = new Color(0.72f, 0.84f, 0.95f, active ? 0.58f : 0.32f);
            }

            if (radarLabels != null)
            {
                Color labelColor = new Color(0.72f, 0.84f, 0.95f, active ? 0.38f : 0.16f);
                for (int i = 0; i < radarLabels.Length; i++)
                {
                    radarLabels[i].color = labelColor;
                }
            }

            for (int i = 0; i < RadarBlipCount; i++)
            {
                if (radarBlipRoots[i] != null)
                {
                    radarBlipRoots[i].gameObject.SetActive(false);
                }
            }

            if (State == GameState.Playing)
            {
                int used = 0;
                for (int i = 0; i < enemies.Count && used < RadarBlipCount; i++)
                {
                    OMTKEnemy enemy = enemies[i];
                    if (enemy == null || !enemy.IsAlive)
                    {
                        continue;
                    }

                    ShowRadarBlip(used, enemy.Azimuth, enemy.Distance, enemy.IsWindingUp, RadarColor(enemy.Variant), enemy.IsStaggered);
                    used++;
                }
            }
            else if (State == GameState.Finale)
            {
                ShowRadarBlip(0, 180f, 4f, finaleFacing, new Color(1f, 0.58f, 0.32f), false);
            }
            else if (State == GameState.Death && fatalClip != null)
            {
                float fatalAzimuth = Mathf.Atan2(fatalPosition.x, fatalPosition.z) * Mathf.Rad2Deg;
                ShowRadarBlip(0, fatalAzimuth, fatalPosition.magnitude, StateTime >= 3.45f, new Color(1f, 0.28f, 0.22f), false);
            }

            UpdateRadarAudio(deltaTime);
        }

        private void SetRadarVisible(bool visible)
        {
            if (radarDial == null || radarVisible == visible)
            {
                return;
            }

            radarVisible = visible;
            radarDial.gameObject.SetActive(visible);
            if (radarReadout != null)
            {
                radarReadout.gameObject.SetActive(visible);
            }

            if (radarLabels != null)
            {
                for (int i = 0; i < radarLabels.Length; i++)
                {
                    if (radarLabels[i] != null)
                    {
                        radarLabels[i].gameObject.SetActive(visible);
                    }
                }
            }

            if (radarBlipRoots != null)
            {
                for (int i = 0; i < radarBlipRoots.Length; i++)
                {
                    if (radarBlipRoots[i] != null && !visible)
                    {
                        radarBlipRoots[i].gameObject.SetActive(false);
                    }
                }
            }
        }

        private void UpdateRadarAudio(float deltaTime)
        {
            if (State != GameState.Playing)
            {
                return;
            }

            radarPingTimer -= deltaTime;
            if (radarPingTimer > 0f)
            {
                return;
            }

            OMTKEnemy nearest = null;
            float nearestDistance = float.MaxValue;
            for (int i = 0; i < enemies.Count; i++)
            {
                OMTKEnemy enemy = enemies[i];
                if (enemy == null || !enemy.IsAlive)
                {
                    continue;
                }

                if (enemy.IsWindingUp)
                {
                    nearest = enemy;
                    nearestDistance = enemy.Distance;
                    break;
                }

                if (enemy.Distance < nearestDistance)
                {
                    nearest = enemy;
                    nearestDistance = enemy.Distance;
                }
            }

            if (nearest == null)
            {
                radarPingTimer = 1.2f;
                return;
            }

            float proximity = 1f - Mathf.Clamp01((nearestDistance - 2.2f) / 8f);
            radarPingTimer = Mathf.Lerp(1.35f, 2.1f, proximity);
            Audio.PlayAt(Audio.RadarPingClip, nearest.Position, 0.06f + 0.07f * proximity, 1f + 0.08f * (1f - proximity));
        }

        private void ShowRadarBlip(int index, float azimuth, float distance, bool winding, Color color, bool staggered)
        {
            if (index < 0 || index >= RadarBlipCount || radarBlipRoots[index] == null || radarBlipCores[index] == null || radarBlipGlows[index] == null)
            {
                return;
            }

            float relative = Mathf.DeltaAngle(PlayerYaw, azimuth);
            float radians = relative * Mathf.Deg2Rad;
            RectTransform root = radarBlipRoots[index];
            root.gameObject.SetActive(true);
            root.anchoredPosition = new Vector2(Mathf.Sin(radians), Mathf.Cos(radians)) * 184f;
            root.localRotation = Quaternion.Euler(0f, 0f, 180f - relative);

            float proximity = 1f - Mathf.Clamp01((distance - 2.2f) / 8f);
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3.2f + index * 1.7f);
            float urgency = winding ? 0.7f + 0.3f * Mathf.Sin(Time.unscaledTime * 12f) : 0.55f + 0.2f * pulse;
            if (staggered)
            {
                urgency *= 0.45f;
            }

            Image core = radarBlipCores[index];
            Color coreColor = color;
            coreColor.a = Mathf.Clamp01(0.52f + 0.38f * proximity + (winding ? 0.12f * urgency : 0f));
            if (staggered)
            {
                coreColor.a *= 0.55f;
            }

            core.color = coreColor;
            float coreScale = 0.9f + 0.22f * proximity + (winding ? 0.28f * urgency : 0f);
            core.rectTransform.localScale = new Vector3(coreScale, coreScale, 1f);

            Image glow = radarBlipGlows[index];
            Color glowColor = color;
            glowColor.a = Mathf.Clamp01(0.08f + 0.2f * proximity + 0.08f * urgency);
            if (staggered)
            {
                glowColor.a *= 0.6f;
            }

            glow.color = glowColor;
            float glowScale = 1.15f + 0.35f * proximity + 0.25f * urgency;
            glow.rectTransform.localScale = new Vector3(glowScale, glowScale, 1f);
        }

        private static Color RadarColor(EnemyVariant variant)
        {
            if (variant == EnemyVariant.Charger)
            {
                return new Color(1f, 0.58f, 0.32f);
            }

            if (variant == EnemyVariant.Conjurer)
            {
                return new Color(0.78f, 0.56f, 1f);
            }

            return new Color(0.58f, 0.88f, 1f);
        }

        private void UpdateHud()
        {
            if (statusText == null)
            {
                return;
            }

            if (State == GameState.Title)
            {
                bool preparingVoice = systemTts != null && !systemTts.IsReady && !systemTts.IsFailed;
                statusText.text = optionsOpen ? "ACCESSIBILITY" : preparingVoice ? "PREPARING VOICE" : "HEADPHONES ON";
            }
            else if (State == GameState.Calibration)
            {
                statusText.text = "CALIBRATION";
            }
            else if (State == GameState.Finale)
            {
                statusText.text = "THE CONDUCTOR";
            }
            else if (State == GameState.Victory)
            {
                statusText.text = "THE HALL IS QUIET";
            }
            else
            {
                statusText.text = "HP " + Hp + "/" + MaxHp + "   WAVE " + Wave + "/3   ATTEMPT " + Attempt;
            }
        }

        private void ShowSubtitle(string text, float duration)
        {
            if (subtitleText == null)
            {
                return;
            }

            subtitleText.text = text;
            subtitleText.color = new Color(1f, 0.9f, 0.58f, 1f);
            subtitleUntil = Time.unscaledTime + duration;
        }

        private void Narrator(string text, float duration)
        {
            ShowSubtitle(text, duration);
            Audio.Speak(text);
        }

        private void LoadProgress()
        {
            Deaths = PlayerPrefs.GetInt(DeathsKey, 0);
            BestWave = PlayerPrefs.GetInt(BestWaveKey, 0);
            EchoMask = PlayerPrefs.GetInt(EchoKey, 0);
            for (int i = 0; i < OMTKConfig.AllEchoes.Length; i++)
            {
                EchoId id = OMTKConfig.AllEchoes[i];
                if (Deaths >= OMTKConfig.RequiredDeaths(id))
                {
                    EchoMask |= OMTKConfig.EchoBit(id);
                }
            }

            RecalculateMaxHp();
        }

        private void SaveProgress()
        {
            PlayerPrefs.SetInt(DeathsKey, Deaths);
            PlayerPrefs.SetInt(BestWaveKey, BestWave);
            PlayerPrefs.SetInt(EchoKey, EchoMask);
            PlayerPrefs.Save();
        }

        private EchoId? UnlockNextEcho()
        {
            for (int i = 0; i < OMTKConfig.AllEchoes.Length; i++)
            {
                EchoId id = OMTKConfig.AllEchoes[i];
                if (!HasEcho(id) && Deaths >= OMTKConfig.RequiredDeaths(id))
                {
                    EchoMask |= OMTKConfig.EchoBit(id);
                    RecalculateMaxHp();
                    SaveProgress();
                    return id;
                }
            }

            return null;
        }

        private void RecalculateMaxHp()
        {
            MaxHp = OMTKConfig.BaseMaxHp + (HasEcho(EchoId.Iron) ? 1 : 0);
        }

        private void CreateCamera()
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(transform, false);
            mainCamera = cameraObject.AddComponent<Camera>();
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.backgroundColor = Color.black;
            mainCamera.cullingMask = 0;
            mainCamera.orthographic = true;
            mainCamera.orthographicSize = 5f;
            mainCamera.nearClipPlane = 0.1f;
            mainCamera.farClipPlane = 100f;
            AudioListener listener = cameraObject.AddComponent<AudioListener>();

            Camera[] cameras = FindObjectsByType<Camera>();
            for (int i = 0; i < cameras.Length; i++)
            {
                if (cameras[i] != mainCamera)
                {
                    Destroy(cameras[i].gameObject);
                }
            }

            AudioListener[] listeners = FindObjectsByType<AudioListener>();
            for (int i = 0; i < listeners.Length; i++)
            {
                if (listeners[i] != listener)
                {
                    Destroy(listeners[i].gameObject);
                }
            }
        }

        private void CreateUI()
        {
            uiFont = LoadFont();

            GameObject canvasObject = new GameObject("Canvas");
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();

            Image background = CreateImage("Background", canvasObject.transform, Color.black);
            Stretch(background.rectTransform);
            background.raycastTarget = false;

            ring = CreateImage("Echo Ring", canvasObject.transform, new Color(0.68f, 0.86f, 0.96f, 0.72f));
            ring.sprite = CreateRingSprite();
            ring.preserveAspect = true;
            ring.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            ring.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            ring.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            ring.rectTransform.anchoredPosition = Vector2.zero;
            ring.rectTransform.sizeDelta = new Vector2(440f, 440f);

            radarDial = CreateImage("Radar Dial", canvasObject.transform, new Color(0.68f, 0.86f, 0.96f, 0.18f));
            radarDial.sprite = CreateRadarDialSprite();
            radarDial.preserveAspect = true;
            CenterRect(radarDial.rectTransform);
            radarDial.rectTransform.sizeDelta = new Vector2(440f, 440f);

            radarBlipRoots = new RectTransform[RadarBlipCount];
            radarBlipCores = new Image[RadarBlipCount];
            radarBlipGlows = new Image[RadarBlipCount];
            Sprite coreSprite = CreateRadarBlipSprite(true);
            Sprite glowSprite = CreateRadarBlipSprite(false);
            for (int i = 0; i < RadarBlipCount; i++)
            {
                GameObject blipObject = new GameObject("Radar Blip " + i, typeof(RectTransform));
                blipObject.transform.SetParent(canvasObject.transform, false);
                RectTransform blipRoot = blipObject.GetComponent<RectTransform>();
                CenterRect(blipRoot);
                blipRoot.sizeDelta = Vector2.zero;

                Image glow = CreateImage("Glow", blipRoot, new Color(1f, 1f, 1f, 0f));
                glow.sprite = glowSprite;
                glow.preserveAspect = true;
                CenterRect(glow.rectTransform);
                glow.rectTransform.sizeDelta = new Vector2(72f, 72f);

                Image core = CreateImage("Core", blipRoot, new Color(1f, 1f, 1f, 0f));
                core.sprite = coreSprite;
                core.preserveAspect = true;
                CenterRect(core.rectTransform);
                core.rectTransform.sizeDelta = new Vector2(26f, 26f);

                radarBlipRoots[i] = blipRoot;
                radarBlipGlows[i] = glow;
                radarBlipCores[i] = core;
                blipObject.SetActive(false);
            }

            string[] radarLabelText = { "000°", "090°", "180°", "270°" };
            RadarLabels(radarLabelText, canvasObject.transform);

            radarReadout = CreateText("Radar Heading", canvasObject.transform, 22, new Color(0.72f, 0.84f, 0.95f, 0.5f), TextAnchor.MiddleCenter);
            CenterRect(radarReadout.rectTransform);
            radarReadout.rectTransform.sizeDelta = new Vector2(240f, 40f);
            radarReadout.text = "HDG 000°";
            SetRadarVisible(OMTKConfig.RadarAssist);

            statusText = CreateText("Status", canvasObject.transform, 32, new Color(0.78f, 0.84f, 0.9f, 0.86f), TextAnchor.UpperLeft);
            statusText.rectTransform.anchorMin = new Vector2(0f, 1f);
            statusText.rectTransform.anchorMax = new Vector2(0f, 1f);
            statusText.rectTransform.pivot = new Vector2(0f, 1f);
            statusText.rectTransform.anchoredPosition = new Vector2(36f, -32f);
            statusText.rectTransform.sizeDelta = new Vector2(880f, 70f);
            statusText.text = "HEADPHONES ON";

            titleText = CreateText("Title", canvasObject.transform, 54, new Color(0.86f, 0.93f, 1f, 0.95f), TextAnchor.MiddleCenter);
            titleText.fontStyle = FontStyle.Bold;
            titleText.rectTransform.anchorMin = new Vector2(0.5f, 0.74f);
            titleText.rectTransform.anchorMax = new Vector2(0.5f, 0.74f);
            titleText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            titleText.rectTransform.anchoredPosition = Vector2.zero;
            titleText.rectTransform.sizeDelta = new Vector2(1000f, 200f);
            titleText.text = "ONE MORE TIME,\nKNIGHT";

            subtitleText = CreateText("Subtitle", canvasObject.transform, 36, new Color(1f, 0.9f, 0.58f, 1f), TextAnchor.MiddleCenter);
            subtitleText.rectTransform.anchorMin = new Vector2(0.5f, 0f);
            subtitleText.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            subtitleText.rectTransform.pivot = new Vector2(0.5f, 0f);
            subtitleText.rectTransform.anchoredPosition = new Vector2(0f, 110f);
            subtitleText.rectTransform.sizeDelta = new Vector2(1000f, 270f);

            promptText = CreateText("Prompt", canvasObject.transform, 36, new Color(0.9f, 0.95f, 1f, 0.9f), TextAnchor.MiddleCenter);
            promptText.rectTransform.anchorMin = new Vector2(0.5f, 0.25f);
            promptText.rectTransform.anchorMax = new Vector2(0.5f, 0.25f);
            promptText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            promptText.rectTransform.anchoredPosition = Vector2.zero;
            promptText.rectTransform.sizeDelta = new Vector2(920f, 100f);
        }

        private Font LoadFont()
        {
            Font font = null;
            try
            {
                font = Font.CreateDynamicFontFromOSFont(new[] { "Arial", "Roboto", "Droid Sans", "Liberation Sans", "DejaVu Sans" }, 48);
            }
            catch
            {
                font = null;
            }

            if (font == null)
            {
                try
                {
                    font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }
                catch
                {
                    font = null;
                }
            }

            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            return font;
        }

        private void RadarLabels(string[] labels, Transform parent)
        {
            radarLabels = new Text[labels.Length];
            for (int i = 0; i < labels.Length; i++)
            {
                float angle = i * 90f * Mathf.Deg2Rad;
                Text label = CreateText("Radar Label " + i, parent, 22, new Color(0.72f, 0.84f, 0.95f, 0.38f), TextAnchor.MiddleCenter);
                CenterRect(label.rectTransform);
                label.rectTransform.anchoredPosition = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * 248f;
                label.rectTransform.sizeDelta = new Vector2(90f, 36f);
                label.text = labels[i];
                radarLabels[i] = label;
            }
        }

        private void CenterRect(RectTransform rect)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
        }

        private Text CreateText(string name, Transform parent, int size, Color color, TextAnchor alignment)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Text text = go.AddComponent<Text>();
            text.font = uiFont;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.lineSpacing = 1.08f;
            text.supportRichText = true;
            text.raycastTarget = false;
            Outline outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            outline.effectDistance = new Vector2(2f, -2f);
            return text;
        }

        private Image CreateImage(string name, Transform parent, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Image image = go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private Sprite CreateRadarDialSprite()
        {
            int size = 256;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            Color32[] pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (x + 0.5f) / size * 2f - 1f;
                    float ny = (y + 0.5f) / size * 2f - 1f;
                    float radius = Mathf.Sqrt(nx * nx + ny * ny);
                    float angle = Mathf.Atan2(nx, ny) * Mathf.Rad2Deg;
                    if (angle < 0f)
                    {
                        angle += 360f;
                    }

                    float alpha = 0f;
                    alpha += Mathf.Exp(-Mathf.Pow((radius - 0.965f) / 0.006f, 2f)) * 0.45f;
                    alpha += Mathf.Exp(-Mathf.Pow((radius - 0.55f) / 0.006f, 2f)) * 0.08f;

                    float nearest = Mathf.Round(angle / 15f) * 15f;
                    float nearest45 = Mathf.Abs(Mathf.Repeat(nearest, 45f));
                    float tickDiff = Mathf.Abs(Mathf.DeltaAngle(angle, nearest));
                    bool cardinal = Mathf.Min(Mathf.Abs(Mathf.Repeat(nearest, 90f)), Mathf.Abs(Mathf.Repeat(nearest, 90f) - 90f)) < 0.1f;
                    bool diagonal = Mathf.Min(nearest45, Mathf.Abs(nearest45 - 45f)) < 0.1f;
                    float tickStrength = cardinal ? 1f : diagonal ? 0.62f : 0.28f;
                    float tickStart = cardinal ? 0.8f : diagonal ? 0.87f : 0.91f;
                    if (tickDiff < 0.6f && radius > tickStart && radius < 0.955f)
                    {
                        alpha = Mathf.Max(alpha, tickStrength * 0.5f);
                    }

                    float axisDiff = Mathf.Min(
                        Mathf.Abs(Mathf.DeltaAngle(angle, 0f)),
                        Mathf.Min(
                            Mathf.Abs(Mathf.DeltaAngle(angle, 90f)),
                            Mathf.Min(Mathf.Abs(Mathf.DeltaAngle(angle, 180f)), Mathf.Abs(Mathf.DeltaAngle(angle, 270f)))));
                    if (axisDiff < 0.18f && radius < 0.78f)
                    {
                        alpha += 0.035f;
                    }

                    alpha = Mathf.Clamp01(alpha);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private Sprite CreateRadarBlipSprite(bool core)
        {
            int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            Color32[] pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (x + 0.5f) / size * 2f - 1f;
                    float ny = (y + 0.5f) / size * 2f - 1f;
                    float alpha;
                    if (core)
                    {
                        float tip = 0.82f;
                        float baseY = -0.58f;
                        float halfWidth = 0.44f * (tip - ny) / (tip - baseY);
                        bool inside = ny >= baseY && ny <= tip && Mathf.Abs(nx) <= halfWidth;
                        if (ny < baseY + 0.16f && Mathf.Abs(nx) < 0.12f)
                        {
                            inside = false;
                        }

                        alpha = inside ? 1f : 0f;
                    }
                    else
                    {
                        float radius = Mathf.Sqrt(nx * nx + ny * ny);
                        alpha = Mathf.Clamp01(1f - radius / 0.95f);
                        alpha = alpha * alpha * 0.9f;
                    }

                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private Sprite CreateRingSprite()
        {
            int size = 256;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            Color32[] pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (x + 0.5f) / size * 2f - 1f;
                    float ny = (y + 0.5f) / size * 2f - 1f;
                    float radius = Mathf.Sqrt(nx * nx + ny * ny);
                    float ring = Mathf.Exp(-Mathf.Pow((radius - 0.82f) / 0.028f, 2f));
                    float inner = Mathf.Exp(-Mathf.Pow((radius - 0.79f) / 0.105f, 2f)) * 0.22f;
                    float glow = Mathf.Clamp01(1f - radius) * 0.1f;
                    float alpha = Mathf.Clamp01(ring * 0.95f + inner + glow);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
