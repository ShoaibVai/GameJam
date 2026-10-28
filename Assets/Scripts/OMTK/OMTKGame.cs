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
        private enum ConductorEdit
        {
            None,
            SwapCast,
            Hasten,
            AddVoice
        }

        public GameState State { get; private set; }
        public int Hp { get; private set; }
        public int MaxHp { get; private set; }
        public int Wave { get; private set; }
        public int Attempt { get; private set; }
        public int Deaths { get; private set; }
        public int BestWave { get; private set; }
        public int CodexMask { get; private set; }
        public int LearnedMask { get; private set; }
        public int RunEchoMask { get; private set; }
        public int ConductorResonance { get; private set; }
        public int EchoMask
        {
            get { return CodexMask; }
        }

        public float PlayerYaw { get; private set; }
        public float StateTime { get; private set; }
        public OMTKAudio Audio { get; private set; }

        public float WindupScale
        {
            get { return 1f; }
        }

        public float PerfectWindow
        {
            get { return HasEcho(EchoId.Grace) ? OMTKConfig.GracePerfectWindow : OMTKConfig.PerfectWindow; }
        }

        public float PerfectParryAngle
        {
            get { return HasEcho(EchoId.Grace) ? OMTKConfig.GracePerfectParryAngle : OMTKConfig.PerfectParryAngle; }
        }

        public bool RehearsalBonusActive
        {
            get { return rehearsalBonusVariant.HasValue && rehearsalBonusAttempt == Attempt; }
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
        private OMTKEnemy primaryToken;
        private OMTKEnemy offbeatToken;
        private OMTKEnemy pendingToken;
        private bool pendingIsOffbeat;
        private float pendingLead;
        private float offbeatDelay;
        private float windupCooldown;
        private bool waitForSpeech;
        private ConductorEdit conductorEdit;
        private Coroutine timeDipRoutine;
        private bool timeDipActive;
        private float guardLockout;
        private float introDuration;
        private float interludeDuration;
        private float subtitleUntil;
        private float parryFlash;
        private float damageFlash;
        private float ringPulse;
        private int finaleBeat;
        private float finaleBearing;
        private bool finaleDriftStarted;
        private int finaleThreatsRemaining;
        private int finaleThreatIndex;
        private bool finaleThreatActive;
        private bool finaleThreatResolved;
        private bool finaleThreatCuePlayed;
        private float finaleThreatStartedAt;
        private float finaleThreatCueAt;
        private float finaleThreatWindowStart;
        private float finaleThreatWindowEnd;
        private float finaleThreatDeadline;
        private float finaleTrueBearing;
        private float finaleDecoyBearing;
        private int finaleShiftDirection = 1;
        private bool finaleFinalDefended;
        private bool finaleFinalCuePlayed;
        private float finaleFinalCueAt;
        private float finaleFinalWindowStart;
        private float finaleFinalWindowEnd;
        private float finaleFinalDeadline;
        private bool finaleEnding;
        private float finaleNextThreatAt;
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
        private EnemyVariant? rehearsalBonusVariant;
        private int rehearsalBonusAttempt;
        private readonly int[] trainingChimeCharges = new int[3];
        private float fatalAzimuth;
        private EnemyVariant fatalVariant;
        private bool deathNotePlayed;
        private bool deathResolved;
        private bool deathAligned;
        private bool deathCueActive;
        private float deathNextCueAt;
        private float deathWindowStart = -1f;
        private float deathWindowEnd = -1f;
        private float deathRetryAt;
        private int deathTapCount;
        private float deathTapDeadline;
        private int offeredBargain = -1;
        private bool bargainOffered;
        private bool bargainAccepted;
        private bool bargainRejected;
        private bool ironShieldAvailable;

        private const string DeathsKey = "OMTK_Deaths";
        private const string BestWaveKey = "OMTK_BestWave";
        private const string EchoKey = "OMTK_EchoMask";
        private const string CodexKey = "OMTK_CodexMask";
        private const string LearnedKey = "OMTK_LearnedMask";
        private const string NarratorKey = "OMTK_NarratorMode";
        private const string HapticsKey = "OMTK_Haptics";
        private const string RadarAssistKey = "OMTK_RadarAssist";
        private const string VolumeKey = "OMTK_Volume";
        private const int OptionsCount = 5;
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
            return (RunEchoMask & OMTKConfig.EchoBit(id)) != 0;
        }

        public float WindupScaleFor(EnemyVariant variant)
        {
            float scale = 1f;
            if (rehearsalBonusVariant.HasValue && rehearsalBonusAttempt == Attempt && rehearsalBonusVariant.Value == variant)
            {
                scale *= OMTKConfig.RehearsalWindupScale;
            }

            if (HasEcho(EchoId.Stillness))
            {
                scale *= 1.12f;
            }

            return scale;
        }

        public bool TryTrainingChime(EnemyVariant variant, Vector3 position)
        {
            int bit = OMTKConfig.CodexBit(variant);
            if ((CodexMask & bit) == 0 || (LearnedMask & bit) != 0)
            {
                return false;
            }

            int index = (int)variant;
            if (index < 0 || index >= trainingChimeCharges.Length || trainingChimeCharges[index] <= 0)
            {
                return false;
            }

            trainingChimeCharges[index]--;
            Audio.PlayAt(Audio.ForesightClip, position, 0.68f, 1.12f);
            return true;
        }

        public void RecordCodexDiscovery(EnemyVariant variant)
        {
            int bit = OMTKConfig.CodexBit(variant);
            if ((CodexMask & bit) != 0)
            {
                return;
            }

            CodexMask |= bit;
            SaveProgress();
        }

        private void ResetTrainingChimes()
        {
            for (int i = 0; i < trainingChimeCharges.Length; i++)
            {
                trainingChimeCharges[i] = OMTKConfig.TrainingChimeCharges;
            }
        }

        private int NextEligibleBargain()
        {
            for (int i = 0; i < OMTKConfig.AllEchoes.Length; i++)
            {
                EchoId id = OMTKConfig.AllEchoes[i];
                if ((RunEchoMask & OMTKConfig.EchoBit(id)) == 0)
                {
                    return (int)id;
                }
            }

            return -1;
        }

        private bool TryAcceptBargain()
        {
            if (!bargainOffered || bargainAccepted || offeredBargain < 0)
            {
                return false;
            }

            EchoId id = (EchoId)offeredBargain;
            RunEchoMask |= OMTKConfig.EchoBit(id);
            ConductorResonance++;
            bargainAccepted = true;
            bargainOffered = false;
            if (id == EchoId.Iron)
            {
                ironShieldAvailable = true;
            }

            Audio.Play2D(Audio.EchoUnlockClip, 0.72f, 1.18f);
            Narrator(OMTKConfig.EchoName(id) + ". " + OMTKConfig.EchoHint(id), 1.9f);
            promptText.text = "TRACE";
            return true;
        }

        private bool TryConsumeIronShield()
        {
            if (!ironShieldAvailable || !HasEcho(EchoId.Iron))
            {
                return false;
            }

            ironShieldAvailable = false;
            RunEchoMask &= ~OMTKConfig.EchoBit(EchoId.Iron);
            return true;
        }

        private void ClearRunBargains()
        {
            RunEchoMask = 0;
            ConductorResonance = 0;
            ironShieldAvailable = false;
        }

        private void RecordPerfectMastery(OMTKEnemy enemy)
        {
            if (enemy == null || enemy.TrainingCuePlayed)
            {
                return;
            }

            int bit = OMTKConfig.CodexBit(enemy.Variant);
            if ((CodexMask & bit) == 0 || (LearnedMask & bit) != 0)
            {
                return;
            }

            LearnedMask |= bit;
            SaveProgress();
            Audio.Play2D(Audio.EchoUnlockClip, 0.7f, 1.2f);
            Narrator(OMTKConfig.CodexName(enemy.Variant) + " mastered. The training tone is gone.", 1.8f);
        }

        public void EnemyAttack(OMTKEnemy enemy)
        {
            if (State != GameState.Playing)
            {
                return;
            }

            CancelTimeDip();
            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i] != enemy)
                {
                    enemies[i].Interrupt();
                }

                enemies[i].CancelDecoy();
            }

            primaryToken = null;
            offbeatToken = null;
            pendingToken = null;
            offbeatDelay = 0f;
            waitForSpeech = false;
            windupCooldown = OMTKConfig.AttackCooldown;
            if (TryConsumeIronShield())
            {
                damageFlash = 0.65f;
                PulseHaptic();
                Audio.Play2D(Audio.ParryClip, 0.9f, 0.82f);
                ShowSubtitle("Iron holds. Once.", 1.05f);
                promptText.text = "LISTEN";
                return;
            }

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
            UpdateAttention();

            for (int i = 0; i < enemies.Count; i++)
            {
                enemies[i].Tick(deltaTime, PlayerYaw, PerfectWindow);
            }

            enemies.RemoveAll(enemy => !enemy.IsAlive);
            UpdateTutorialDeath(deltaTime);
            if (State != GameState.Playing)
            {
                return;
            }

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

            UpdateTokenScheduler(deltaTime);
        }

        private void UpdateAttention()
        {
            OMTKEnemy attended = null;
            float closest = 361f;
            for (int i = 0; i < enemies.Count; i++)
            {
                OMTKEnemy enemy = enemies[i];
                if (enemy == null || !enemy.IsAlive)
                {
                    continue;
                }

                float angle = Mathf.Abs(Mathf.DeltaAngle(PlayerYaw, enemy.Azimuth));
                if (angle < closest)
                {
                    closest = angle;
                    attended = enemy;
                }
            }

            for (int i = 0; i < enemies.Count; i++)
            {
                OMTKEnemy enemy = enemies[i];
                if (enemy == null)
                {
                    continue;
                }

                enemy.SetAttention(enemy == attended || enemies.Count == 1 ? 1f : 0.35f);
            }
        }

        private void UpdateTokenScheduler(float deltaTime)
        {
            if (primaryToken != null && (!primaryToken.IsAlive || !primaryToken.IsWindingUp))
            {
                primaryToken = null;
            }

            if (offbeatToken != null && (!offbeatToken.IsAlive || !offbeatToken.IsWindingUp))
            {
                offbeatToken = null;
            }

            if (pendingToken != null && !pendingToken.IsAlive)
            {
                pendingToken = null;
            }

            if (pendingToken != null)
            {
                pendingLead -= deltaTime;
                if (pendingLead <= 0f)
                {
                    OMTKEnemy enemy = pendingToken;
                    bool offbeat = pendingIsOffbeat;
                    pendingToken = null;
                    LaunchWindup(enemy, offbeat);
                }
            }

            if (offbeatDelay > 0f)
            {
                offbeatDelay -= deltaTime;
                if (offbeatDelay <= 0f)
                {
                    offbeatDelay = 0f;
                    OMTKEnemy offbeat = FindAtTarget(null, primaryToken);
                    if (offbeat != null)
                    {
                        QueueWindup(offbeat, true);
                    }
                }
            }

            if (primaryToken != null || offbeatToken != null || pendingToken != null)
            {
                return;
            }

            if (waitForSpeech)
            {
                if (!Audio.IsSpeechActive)
                {
                    waitForSpeech = false;
                }

                return;
            }

            windupCooldown -= deltaTime;
            if (windupCooldown > 0f)
            {
                return;
            }

            OMTKEnemy chosen = FindAtTarget(null, null);
            if (chosen != null)
            {
                QueueWindup(chosen, false);
            }
        }

        private OMTKEnemy FindAtTarget(OMTKEnemy excludeA, OMTKEnemy excludeB)
        {
            for (int i = 0; i < enemies.Count; i++)
            {
                OMTKEnemy enemy = enemies[i];
                if (enemy == null || !enemy.IsAlive || enemy == excludeA || enemy == excludeB)
                {
                    continue;
                }

                if (enemy.AtTarget)
                {
                    return enemy;
                }
            }

            return null;
        }

        private void QueueWindup(OMTKEnemy enemy, bool offbeat)
        {
            if (enemy == null)
            {
                return;
            }

            if (enemy.Variant == EnemyVariant.Conjurer && enemy.BeginDecoyWindup())
            {
                pendingToken = enemy;
                pendingIsOffbeat = offbeat;
                pendingLead = OMTKConfig.ConjurerDecoyLead;
                return;
            }

            LaunchWindup(enemy, offbeat);
        }

        private void LaunchWindup(OMTKEnemy enemy, bool offbeat)
        {
            if (enemy == null || !enemy.BeginWindup())
            {
                return;
            }

            if (offbeat)
            {
                offbeatToken = enemy;
            }
            else
            {
                primaryToken = enemy;
                offbeatDelay = Wave >= 3 || Attempt >= 3 ? OMTKConfig.OffbeatWindupDelay : 0f;
            }

            RememberLastCue(enemy.WindupClip, enemy.Position, 0.9f, 1f, false);
        }

        private void StartRun()
        {
            optionsOpen = false;
            Attempt = 1;
            conductorEdit = ConductorEdit.None;
            Wave = 1;
            Hp = MaxHp;
            RunEchoMask = 0;
            ConductorResonance = 0;
            ironShieldAvailable = false;
            rehearsalBonusVariant = null;
            rehearsalBonusAttempt = 0;
            ResetTrainingChimes();
            ClearDeathRehearsal();
            guardLockout = 0f;
            calibrationDone = false;
            calibrationAlignedAnnounced = false;
            waitForSpeech = false;
            ClearTutorial();
            ClearEnemies();
            Audio.StopTinnitus();
            Audio.StopCalibration();
            Audio.Unduck();
            Audio.PlayMusic();
            Audio.SetTension(false);
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
            RollConductorEdit();
            Wave = 1;
            Hp = MaxHp;
            ResetTrainingChimes();
            ClearDeathRehearsal();
            waitForSpeech = false;
            guardLockout = 0f;
            ClearTutorial();
            ClearEnemies();
            Audio.StopTinnitus();
            Audio.StopCalibration();
            Audio.Unduck();
            Audio.PlayMusic();
            Audio.SetTension(false);
            Audio.StopConductor();
            RecenterYaw();
            introDuration = 1.65f;
            SetState(GameState.Intro);
            string hint = ConductorHint();
            ShowSubtitle("Again. " + hint, 1.8f);
            promptText.text = "LISTEN";
            Audio.Speak("Again. " + hint);
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
            CancelTimeDip();
            primaryToken = null;
            offbeatToken = null;
            pendingToken = null;
            offbeatDelay = 0f;
            waitForSpeech = true;
            windupCooldown = OMTKConfig.WaveStartCooldown;
            SetState(GameState.Playing);
            RecenterYaw();
            Audio.SetTension(wave >= 3);

            if (wave == 1)
            {
                float bearing = Attempt == 1
                    ? OMTKConfig.CalibrationAzimuth
                    : RandomizedBearing(62f, OMTKConfig.StalkerBearingJitter);
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
                float bearing = Attempt == 1
                    ? -74f
                    : RandomizedBearing(-74f, OMTKConfig.ChargerBearingJitter);
                EnemyVariant variant = conductorEdit == ConductorEdit.SwapCast ? EnemyVariant.Stalker : EnemyVariant.Charger;
                Spawn(variant, bearing);
                if (Attempt >= 3)
                {
                    Spawn(EnemyVariant.Stalker, Mathf.Repeat(bearing + 180f, 360f));
                }

                Narrator("Wave two. It learns your rhythm.", 2.25f);
            }
            else
            {
                float firstBearing = Attempt == 1
                    ? 142f
                    : RandomizedBearing(142f, OMTKConfig.ConjurerBearingJitter);
                float secondBearing = Attempt == 1
                    ? -38f
                    : EnsureSeparation(firstBearing, RandomizedBearing(-38f, OMTKConfig.StalkerBearingJitter), OMTKConfig.MinEnemySeparation);
                EnemyVariant secondVariant = conductorEdit == ConductorEdit.SwapCast ? EnemyVariant.Charger : EnemyVariant.Stalker;
                Spawn(EnemyVariant.Conjurer, firstBearing);
                Spawn(secondVariant, secondBearing);

                if (Attempt >= 3 || conductorEdit == ConductorEdit.AddVoice)
                {
                    Spawn(EnemyVariant.Stalker, FindOpenBearing(firstBearing, secondBearing));
                }

                Narrator("Wave three. Do not trust the silence.", 2.25f);
            }

            float approachScale = 1f + Mathf.Min(0.3f, (Attempt - 1) * 0.07f);
            if (conductorEdit == ConductorEdit.Hasten)
            {
                approachScale += 0.18f;
            }

            for (int i = 0; i < enemies.Count; i++)
            {
                enemies[i].SetApproachScale(approachScale);
            }

            promptText.text = "LISTEN";
            titleText.gameObject.SetActive(false);
        }

        private void RollConductorEdit()
        {
            if (Attempt < 2)
            {
                conductorEdit = ConductorEdit.None;
                return;
            }

            conductorEdit = (ConductorEdit)Random.Range(1, 3 + 1);
        }

        private string ConductorHint()
        {
            if (conductorEdit == ConductorEdit.SwapCast)
            {
                return "The Conductor changes the cast.";
            }

            if (conductorEdit == ConductorEdit.Hasten)
            {
                return "The Conductor hastens the approach.";
            }

            if (conductorEdit == ConductorEdit.AddVoice)
            {
                return "The Conductor adds a voice.";
            }

            return "Listen.";
        }

        private static float RandomizedBearing(float baseBearing, float jitter)
        {
            return Mathf.Repeat(baseBearing + Random.Range(-jitter, jitter), 360f);
        }

        private static float EnsureSeparation(float first, float second, float minimum)
        {
            float delta = Mathf.Abs(Mathf.DeltaAngle(first, second));
            if (delta >= minimum)
            {
                return second;
            }

            return Mathf.Repeat(first + minimum, 360f);
        }

        private static float FindOpenBearing(float first, float second)
        {
            float best = 0f;
            float bestScore = -1f;
            for (int i = 0; i < 72; i++)
            {
                float candidate = i * 5f;
                float score = Mathf.Min(
                    Mathf.Abs(Mathf.DeltaAngle(candidate, first)),
                    Mathf.Abs(Mathf.DeltaAngle(candidate, second)));
                if (score > bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }

            return best;
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
                    primaryToken = tutorialKiller;
                    RememberLastCue(primaryToken.WindupClip, primaryToken.Position, 0.9f, 1f, false);
                    ShowSubtitle("The first cut is inevitable.", 1.1f);
                    promptText.text = "LISTEN";
                }
            }
        }

        private void StartInterlude()
        {
            SetState(GameState.Interlude);
            Audio.SetTension(false);
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
            Audio.StopConductor();
            SetState(GameState.Finale);
            Audio.SetTension(true);
            Audio.Duck(0.82f);
            Audio.PlayMusic();
            finaleBeat = 1;
            finaleBearing = 0f;
            finaleDriftStarted = false;
            finaleThreatsRemaining = 0;
            finaleThreatIndex = 0;
            finaleThreatActive = false;
            finaleThreatResolved = false;
            finaleThreatCuePlayed = false;
            finaleNextThreatAt = 0f;
            finaleFinalDefended = false;
            finaleFinalCuePlayed = false;
            finaleEnding = false;
            ShowSubtitle("The Conductor speaks.", 2.2f);
            promptText.text = "LISTEN";
            Audio.Speak("You learned every echo but mine. Listen.");
        }

        private void UpdateFinale()
        {
            if (finaleEnding)
            {
                return;
            }

            if (finaleBeat == 1)
            {
                UpdateFinaleDrift();
            }
            else if (finaleBeat == 2)
            {
                UpdateFinaleHarvest();
            }
            else
            {
                UpdateFinaleClimax();
            }
        }

        private void UpdateFinaleDrift()
        {
            if (!finaleDriftStarted && StateTime >= OMTKConfig.FinaleDriftStart)
            {
                finaleDriftStarted = true;
                Audio.StartConductor(Vector3.forward * 4f);
                Audio.PlayConductorSpatial("Do you hear me now?", 0f);
            }

            if (!finaleDriftStarted)
            {
                return;
            }

            float progress = Mathf.Clamp01((StateTime - OMTKConfig.FinaleDriftStart) / OMTKConfig.FinaleDriftDuration);
            finaleBearing = Mathf.LerpAngle(0f, 180f, progress);
            Audio.UpdateConductor(PlayerYaw, finaleBearing);
            if (progress >= 1f)
            {
                StartFinaleHarvest();
            }
        }

        private void StartFinaleHarvest()
        {
            finaleBeat = 2;
            finaleThreatsRemaining = 1 + ConductorResonance;
            finaleThreatIndex = 0;
            finaleTrueBearing = 180f;
            finaleShiftDirection = 1;
            finaleBearing = finaleTrueBearing;
            Audio.SetConductorBearing(finaleBearing);
            Audio.PlayConductorSpatial("You gave me this.", finaleBearing);
            ShowSubtitle("The harvest begins.", 1.2f);
            StartFinaleThreat();
        }

        private void StartFinaleThreat()
        {
            if (finaleThreatsRemaining <= 0)
            {
                BeginFinaleClimax();
                return;
            }

            finaleThreatActive = true;
            finaleThreatResolved = false;
            finaleThreatCuePlayed = false;
            finaleThreatStartedAt = StateTime;
            finaleThreatCueAt = StateTime + OMTKConfig.FinaleThreatLead;
            finaleThreatWindowStart = StateTime + OMTKConfig.FinaleThreatWindowDelay;
            finaleThreatWindowEnd = finaleThreatWindowStart + OMTKConfig.FinaleThreatWindowDuration;
            finaleThreatDeadline = StateTime + OMTKConfig.FinaleThreatDeadline;
            finaleDecoyBearing = Mathf.Repeat(finaleTrueBearing + (finaleThreatIndex % 2 == 0 ? 90f : -90f), 360f);
            finaleBearing = finaleTrueBearing;
            Audio.SetConductorBearing(finaleBearing);
            ShowSubtitle(DeceptiveFinalePrompt(), 0.95f);
            promptText.text = "LISTEN";
        }

        private string DeceptiveFinalePrompt()
        {
            int choice = finaleThreatIndex % 3;
            if (choice == 0)
            {
                return "STRIKE FRONT";
            }

            if (choice == 1)
            {
                return "TURN LEFT";
            }

            return "BEHIND YOU";
        }

        private void UpdateFinaleHarvest()
        {
            if (!finaleThreatActive)
            {
                if (finaleNextThreatAt > 0f && StateTime >= finaleNextThreatAt)
                {
                    if (finaleThreatsRemaining <= 0)
                    {
                        BeginFinaleClimax();
                    }
                    else
                    {
                        finaleThreatIndex++;
                        StartFinaleThreat();
                    }
                }

                return;
            }

            if (!finaleThreatCuePlayed && StateTime >= finaleThreatCueAt)
            {
                finaleThreatCuePlayed = true;
                Audio.PlayAt(Audio.WindupConjurer, FinalePosition(finaleTrueBearing), 0.95f, 0.92f, false);
                AudioClip decoyClip = fatalClip != null ? fatalClip : Audio.DecoyWindupClip;
                Audio.PlayAt(decoyClip, FinalePosition(finaleDecoyBearing), 0.45f, 0.82f, false);
                ShowSubtitle(DeceptiveFinalePrompt(), 0.95f);
            }

            promptText.text = StateTime >= finaleThreatWindowStart && StateTime <= finaleThreatWindowEnd ? "TAP" : "LISTEN";
            if (StateTime >= finaleThreatDeadline && !finaleThreatResolved)
            {
                ResolveFinaleThreat(false, "The opening closes.");
            }
        }

        private void ResolveFinaleThreat(bool success, string failure)
        {
            if (!finaleThreatActive || finaleThreatResolved)
            {
                return;
            }

            finaleThreatResolved = true;
            finaleThreatActive = false;
            if (success)
            {
                Audio.Play2D(Audio.PerfectClip, 0.85f, 1.05f);
                PulseHaptic();
                ShowSubtitle("Deflected. The source moves.", 0.9f);
                finaleTrueBearing = Mathf.Repeat(finaleTrueBearing + 90f * finaleShiftDirection, 360f);
                finaleShiftDirection *= -1;
                finaleThreatsRemaining--;
                finaleNextThreatAt = StateTime + 0.75f;
            }
            else
            {
                PenalizeFinale(failure);
                if (State != GameState.Finale || finaleBeat != 2)
                {
                    return;
                }

                finaleThreatsRemaining--;
                finaleNextThreatAt = StateTime + 0.75f;
            }
        }

        private void TryFinaleParry()
        {
            if (State != GameState.Finale || finaleEnding)
            {
                return;
            }

            if (finaleBeat == 3)
            {
                if (finaleFinalDefended)
                {
                    ShowSubtitle("Swipe toward the voice.", 0.8f);
                    return;
                }

                if (StateTime < finaleFinalWindowStart || StateTime > finaleFinalWindowEnd)
                {
                    ShowSubtitle("Wait for the opening.", 0.75f);
                    return;
                }

                float finalAngle = Mathf.Abs(Mathf.DeltaAngle(PlayerYaw, finaleTrueBearing));
                if (finalAngle > OMTKConfig.GlancingDeflectAngle)
                {
                    PenalizeFinale("Off-axis. The Conductor turns.");
                    if (State == GameState.Finale && finaleBeat == 3)
                    {
                        ResetFinaleClimaxCue();
                    }

                    return;
                }

                finaleFinalDefended = true;
                Audio.Play2D(Audio.PerfectClip, 0.9f, 1.05f);
                PulseHaptic();
                ShowSubtitle("Now. Swipe.", 1.1f);
                promptText.text = "SWIPE";
                return;
            }

            if (finaleBeat != 2 || !finaleThreatActive || finaleThreatResolved)
            {
                return;
            }

            if (StateTime < finaleThreatWindowStart)
            {
                ResolveFinaleThreat(false, "Too soon. The Conductor smiles.");
                return;
            }

            float angle = Mathf.Abs(Mathf.DeltaAngle(PlayerYaw, finaleTrueBearing));
            if (angle > OMTKConfig.GlancingDeflectAngle)
            {
                ResolveFinaleThreat(false, "Off-axis. The source is elsewhere.");
                return;
            }

            if (StateTime > finaleThreatWindowEnd)
            {
                ResolveFinaleThreat(false, "The opening closes.");
                return;
            }

            ResolveFinaleThreat(true, string.Empty);
        }

        private void TryFinaleSwipe()
        {
            if (State != GameState.Finale || finaleEnding)
            {
                return;
            }

            if (finaleBeat == 1)
            {
                ShowSubtitle("Nothing to cut yet.", 0.75f);
                return;
            }

            if (finaleBeat == 2 && finaleThreatActive && !finaleThreatResolved)
            {
                float trueAngle = Mathf.Abs(Mathf.DeltaAngle(PlayerYaw, finaleTrueBearing));
                float decoyAngle = Mathf.Abs(Mathf.DeltaAngle(PlayerYaw, finaleDecoyBearing));
                if (trueAngle > OMTKConfig.FinaleOffAxisAngle && decoyAngle <= OMTKConfig.StandardParryAngle)
                {
                    ResolveFinaleThreat(false, "You struck a decoy.");
                }
                else if (trueAngle > OMTKConfig.FinaleOffAxisAngle)
                {
                    ResolveFinaleThreat(false, "The blade finds only air.");
                }
                else
                {
                    ResolveFinaleThreat(false, "The Conductor is not open.");
                }

                return;
            }

            if (finaleBeat != 3 || !finaleFinalDefended)
            {
                ShowSubtitle("Defend first.", 0.75f);
                return;
            }

            float angle = Mathf.Abs(Mathf.DeltaAngle(PlayerYaw, finaleTrueBearing));
            if (angle > OMTKConfig.FinaleFinalSwipeAngle)
            {
                Audio.Play2D(Audio.OffAxisClip, 0.55f);
                ShowSubtitle("Wrong line. The Conductor laughs.", 1f);
                PenalizeFinale("Wrong line.");
                if (State == GameState.Finale && finaleBeat == 3)
                {
                    ResetFinaleClimaxCue();
                }

                return;
            }

            BeginVictorySequence();
        }

        private void BeginFinaleClimax()
        {
            finaleBeat = 3;
            finaleThreatActive = false;
            finaleFinalDefended = false;
            finaleFinalCuePlayed = false;
            finaleTrueBearing = 180f;
            finaleBearing = 180f;
            Audio.SetConductorBearing(180f);
            finaleFinalCueAt = StateTime + 1f;
            finaleFinalWindowStart = StateTime + OMTKConfig.FinaleThreatWindowDelay;
            finaleFinalWindowEnd = finaleFinalWindowStart + OMTKConfig.FinaleThreatWindowDuration;
            finaleFinalDeadline = StateTime + OMTKConfig.FinaleThreatDeadline;
            Audio.PlayConductorSpatial("One last time.", 180f);
            ShowSubtitle("The final harvest.", 1.1f);
            promptText.text = "LISTEN";
        }

        private void UpdateFinaleClimax()
        {
            if (!finaleFinalCuePlayed && StateTime >= finaleFinalCueAt)
            {
                finaleFinalCuePlayed = true;
                Audio.PlayAt(Audio.WindupConjurer, FinalePosition(finaleTrueBearing), 1f, 0.9f, false);
                ShowSubtitle("Every echo at once.", 0.9f);
            }

            if (!finaleFinalDefended)
            {
                promptText.text = StateTime >= finaleFinalWindowStart && StateTime <= finaleFinalWindowEnd ? "TAP" : "LISTEN";
            }

            if (!finaleFinalDefended && StateTime >= finaleFinalDeadline)
            {
                PenalizeFinale("The final harvest breaks your guard.");
                if (Hp > 0)
                {
                    ResetFinaleClimaxCue();
                }
            }
        }

        private void ResetFinaleClimaxCue()
        {
            finaleFinalDefended = false;
            finaleFinalCuePlayed = false;
            finaleFinalCueAt = StateTime + 0.8f;
            finaleFinalWindowStart = StateTime + OMTKConfig.FinaleThreatWindowDelay;
            finaleFinalWindowEnd = finaleFinalWindowStart + OMTKConfig.FinaleThreatWindowDuration;
            finaleFinalDeadline = StateTime + OMTKConfig.FinaleThreatDeadline;
        }

        private void PenalizeFinale(string message)
        {
            if (finaleEnding)
            {
                return;
            }

            if (TryConsumeIronShield())
            {
                ShowSubtitle("Iron holds. Once.", 1f);
                return;
            }

            Hp--;
            damageFlash = 1f;
            PulseHaptic();
            Audio.Play2D(Audio.HitClip, 0.9f, 0.94f);
            ShowSubtitle(message, 1.1f);
            Audio.Speak(message);
            if (Hp <= 0)
            {
                RestartFinaleAtOneHp();
            }
        }

        private void RestartFinaleAtOneHp()
        {
            Hp = 1;
            Audio.StopConductor();
            Audio.Unduck();
            Audio.StopVoice();
            StartFinale();
            ShowSubtitle("Not yet. Again.", 1.6f);
            Audio.Speak("Not yet. Again.");
        }

        private void BeginVictorySequence()
        {
            if (finaleEnding)
            {
                return;
            }

            finaleEnding = true;
            finaleThreatActive = false;
            Audio.SilenceForVictory();
            StartCoroutine(FinishVictorySequence());
        }

        private IEnumerator FinishVictorySequence()
        {
            yield return new WaitForSecondsRealtime(1.5f);
            Audio.Play2D(Audio.SheatheClip, 0.85f);
            yield return new WaitForSecondsRealtime(0.45f);
            Victory();
        }

        private Vector3 FinalePosition(float bearing)
        {
            return Quaternion.Euler(0f, bearing, 0f) * Vector3.forward * 4f;
        }

        private void TryParry()
        {
            if (State != GameState.Playing || guardLockout > 0f)
            {
                return;
            }

            if (tutorialDeathActive && tutorialWindupStarted && primaryToken == tutorialKiller)
            {
                Audio.Play2D(Audio.OffAxisClip, 0.35f);
                ShowSubtitle("This blow cannot be stopped.", 1f);
                return;
            }

            OMTKEnemy target = FindParryTarget(out bool targetWasOffbeat);
            if (target != null)
            {
                float angle = Mathf.Abs(Mathf.DeltaAngle(PlayerYaw, target.Azimuth));
                if (angle > OMTKConfig.GlancingDeflectAngle)
                {
                    Audio.Play2D(Audio.OffAxisClip, 0.5f);
                    ShowSubtitle("Wide. Face the sound.", 0.9f);
                    promptText.text = "TURN";
                    TriggerPrematureTap();
                    return;
                }

                if (target.WindupRemaining > OMTKConfig.StandardParryWindow)
                {
                    TriggerPrematureTap();
                    if (targetWasOffbeat)
                    {
                        target.Interrupt();
                        target.CancelDecoy();
                        offbeatToken = null;
                    }

                    return;
                }

                ParryGrade grade = ParryGrade.Standard;
                if (angle > OMTKConfig.StandardParryAngle)
                {
                    grade = ParryGrade.Glancing;
                }
                else if (angle <= PerfectParryAngle && target.WindupRemaining <= PerfectWindow && target.IsResonanceLocked)
                {
                    grade = ParryGrade.Perfect;
                }

                target.Parry(grade);
                if (targetWasOffbeat)
                {
                    offbeatToken = null;
                }
                else
                {
                    primaryToken = null;
                    offbeatDelay = 0f;
                }

                if (grade == ParryGrade.Perfect)
                {
                    RecordPerfectMastery(target);
                }

                float baseCooldown = OMTKConfig.AttackCooldown;
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
                    if (!targetWasOffbeat)
                    {
                        TriggerOffbeatResponse(target);
                    }
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

        private OMTKEnemy FindParryTarget(out bool targetWasOffbeat)
        {
            targetWasOffbeat = false;
            OMTKEnemy best = null;
            float bestAngle = float.MaxValue;
            bool bestWasOffbeat = false;
            if (primaryToken != null && primaryToken.IsWindingUp)
            {
                best = primaryToken;
                bestAngle = Mathf.Abs(Mathf.DeltaAngle(PlayerYaw, best.Azimuth));
            }

            if (offbeatToken != null && offbeatToken != primaryToken && offbeatToken.IsWindingUp)
            {
                float angle = Mathf.Abs(Mathf.DeltaAngle(PlayerYaw, offbeatToken.Azimuth));
                if (best == null || angle < bestAngle - 0.1f)
                {
                    best = offbeatToken;
                    bestAngle = angle;
                    bestWasOffbeat = true;
                }
            }

            targetWasOffbeat = bestWasOffbeat;
            return best;
        }

        private void TriggerOffbeatResponse(OMTKEnemy source)
        {
            if (offbeatToken == null || offbeatToken == source || !offbeatToken.IsWindingUp)
            {
                return;
            }

            if (!timeDipActive)
            {
                timeDipActive = true;
                timeDipRoutine = StartCoroutine(TimeDipRoutine());
            }
        }

        private IEnumerator TimeDipRoutine()
        {
            Audio.Duck(0.62f);
            Time.timeScale = OMTKConfig.OffbeatTimeScale;
            yield return new WaitForSecondsRealtime(OMTKConfig.OffbeatTimeDipDuration);
            Time.timeScale = 1f;
            timeDipActive = false;
            timeDipRoutine = null;
            Audio.Unduck();
        }

        private void CancelTimeDip()
        {
            if (timeDipRoutine != null)
            {
                StopCoroutine(timeDipRoutine);
                timeDipRoutine = null;
            }

            if (timeDipActive)
            {
                timeDipActive = false;
                Time.timeScale = 1f;
                Audio.Unduck();
            }
        }

        private void StartDeath(OMTKEnemy killer)
        {
            fatalVariant = killer != null ? killer.Variant : EnemyVariant.Stalker;
            fatalClip = killer != null ? killer.WindupClip : Audio.DeathClip;
            fatalAzimuth = killer != null ? killer.Azimuth : Mathf.Repeat(PlayerYaw + 180f, 360f);
            fatalPosition = killer != null ? killer.Position : Quaternion.Euler(0f, fatalAzimuth, 0f) * Vector3.forward * 2.3f;
            currentDeathTutorial = tutorialDeathActive;
            ClearEnemies();
            SetState(GameState.Death);
            Deaths++;
            BestWave = Mathf.Max(BestWave, Wave);
            deathNotePlayed = false;
            deathResolved = false;
            deathAligned = false;
            deathCueActive = false;
            deathNextCueAt = OMTKConfig.DeathCueFirstTime;
            deathWindowStart = -1f;
            deathWindowEnd = -1f;
            deathRetryAt = 0f;
            deathTapCount = 0;
            deathTapDeadline = 0f;
            waitForSpeech = false;
            offeredBargain = NextEligibleBargain();
            bargainOffered = offeredBargain >= 0;
            bargainAccepted = false;
            bargainRejected = false;
            Audio.StopTinnitus();
            Audio.SetTension(false);
            Audio.Unduck();
            Audio.Duck(0.24f);
            SaveProgress();
            ShowSubtitle("You died.", 1.1f);
            promptText.text = "LISTEN";
            Audio.Speak("You died. Trace the blow.");
        }

        private void UpdateDeath()
        {
            if (StateTime >= OMTKConfig.DeathNoteTime && !deathNotePlayed)
            {
                deathNotePlayed = true;
                Audio.PlayAt(Audio.DeathClip, fatalPosition, 0.7f, OMTKConfig.DeathCuePitch, true);
            }

            if (deathResolved)
            {
                if (StateTime >= deathRetryAt)
                {
                    ClearDeathRehearsal();
                    Retry();
                }

                return;
            }

            if (deathTapCount > 0 && StateTime > deathTapDeadline)
            {
                deathTapCount = 0;
            }

            float angle = Mathf.Abs(Mathf.DeltaAngle(PlayerYaw, fatalAzimuth));
            deathAligned = angle <= OMTKConfig.DeathRehearsalAngle;

            if (!deathCueActive && StateTime >= deathNextCueAt && StateTime < OMTKConfig.DeathFallbackDuration)
            {
                PlayDeathReplay();
            }

            if (deathCueActive)
            {
                if (StateTime < deathWindowStart)
                {
                    promptText.text = deathAligned ? "WAIT" : "TURN";
                }
                else if (StateTime <= deathWindowEnd)
                {
                    if (deathAligned)
                    {
                        promptText.text = "TAP";
                        if (!bargainRejected && bargainOffered && offeredBargain >= 0)
                        {
                            bargainRejected = true;
                            bargainOffered = false;
                            ShowSubtitle("The echo waits. Tap to answer it.", 0.65f);
                        }
                    }
                    else
                    {
                        promptText.text = "TURN";
                    }
                }
                else
                {
                    deathCueActive = false;
                    deathNextCueAt += OMTKConfig.DeathCueInterval;
                    promptText.text = "LISTEN";
                }
            }

            if (StateTime >= OMTKConfig.DeathFallbackDuration)
            {
                deathResolved = true;
                deathCueActive = false;
                deathRetryAt = Mathf.Min(StateTime + OMTKConfig.DeathSuccessDelay, 5f);
                ShowSubtitle("The echo fades. Again.", 1.2f);
                Audio.Speak("The echo fades. Again.");
            }
        }

        private void PlayDeathReplay()
        {
            deathCueActive = true;
            deathWindowStart = StateTime + OMTKConfig.DeathCueWindowDelay;
            deathWindowEnd = deathWindowStart + OMTKConfig.DeathCueWindowDuration;
            Audio.Duck(0.18f);
            Audio.PlayAt(fatalClip != null ? fatalClip : Audio.DeathClip, fatalPosition, 1f, OMTKConfig.DeathCuePitch, true);
            PulseHaptic();
        }

        private void TryDeathRehearsalTap()
        {
            if (State != GameState.Death || deathResolved)
            {
                return;
            }

            if (deathCueActive && StateTime >= deathWindowStart && StateTime <= deathWindowEnd)
            {
                if (deathAligned)
                {
                    FinishDeathRehearsal(true);
                }
                else
                {
                    if (bargainOffered && !bargainAccepted && deathTapCount > 0 && StateTime <= deathTapDeadline)
                    {
                        TryAcceptBargainBySwipe();
                        return;
                    }

                    deathTapCount = 1;
                    deathTapDeadline = StateTime + TwoFingerTapWindow;
                    Audio.Play2D(Audio.OffAxisClip, 0.55f);
                    ShowSubtitle("Not aligned. Trace the bearing.", 0.85f);
                }

                return;
            }

            if (bargainOffered && !bargainAccepted)
            {
                if (deathTapCount > 0 && StateTime <= deathTapDeadline)
                {
                    TryAcceptBargainBySwipe();
                    return;
                }

                deathTapCount = 1;
                deathTapDeadline = StateTime + TwoFingerTapWindow;
                ShowSubtitle("Double-tap for the bargain.", 0.9f);
                return;
            }

            Audio.Play2D(Audio.WhiffClip, 0.4f);
            ShowSubtitle("Wait for the blow.", 0.7f);
        }

        private bool TryAcceptBargainBySwipe()
        {
            if (deathResolved || !TryAcceptBargain())
            {
                return false;
            }

            bargainRejected = false;
            ShowSubtitle("Bargain taken. Now trace the blow.", 1.35f);
            Audio.Speak("Bargain taken. Now trace the blow.");
            return true;
        }

        private void FinishDeathRehearsal(bool success)
        {
            deathResolved = true;
            deathCueActive = false;
            if (success)
            {
                bargainRejected = true;
                bargainOffered = false;
                RecordCodexDiscovery(fatalVariant);
                rehearsalBonusVariant = fatalVariant;
                rehearsalBonusAttempt = Attempt;
                SaveProgress();
                deathRetryAt = Mathf.Min(StateTime + OMTKConfig.DeathSuccessDelay, 5f);
                ShowSubtitle("You remembered. It will be slower.", 1.45f);
                Audio.Play2D(Audio.EchoUnlockClip, 0.7f, 1.2f);
                Audio.Speak("You remembered.");
            }
            else
            {
                deathRetryAt = Mathf.Min(StateTime + OMTKConfig.DeathSuccessDelay, 5f);
                ShowSubtitle("The echo fades. Again.", 1.2f);
                Audio.Speak("The echo fades. Again.");
            }
        }

        private void SkipDeathRehearsal()
        {
            if (State != GameState.Death || deathResolved)
            {
                return;
            }

            deathResolved = true;
            deathCueActive = false;
            deathRetryAt = Mathf.Min(StateTime + OMTKConfig.DeathSuccessDelay, 5f);
            ShowSubtitle("Rehearsal skipped.", 0.9f);
            Audio.Speak("Rehearsal skipped.");
        }

        private void ClearDeathRehearsal()
        {
            deathCueActive = false;
            deathResolved = false;
            deathAligned = false;
            deathNotePlayed = false;
            deathRetryAt = 0f;
            deathTapCount = 0;
            deathTapDeadline = 0f;
            offeredBargain = -1;
            bargainOffered = false;
            bargainAccepted = false;
            bargainRejected = false;
            deathWindowStart = -1f;
            deathWindowEnd = -1f;
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

            if (primaryToken == enemy)
            {
                primaryToken = null;
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
            ClearRunBargains();
            rehearsalBonusVariant = null;
            rehearsalBonusAttempt = 0;
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
            Audio.PlayConductorSpatial("You remembered.", 180f);
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
            primaryToken = null;
            offbeatToken = null;
            pendingToken = null;
            offbeatDelay = 0f;
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

                if (keyboard.upArrowKey.wasPressedThisFrame)
                {
                    HandleSwipeUp();
                }

                if (keyboard.oKey.wasPressedThisFrame && State == GameState.Title)
                {
                    ToggleOptions();
                }

                if (keyboard.escapeKey.wasPressedThisFrame && optionsOpen)
                {
                    ToggleOptions();
                }
                else if (keyboard.escapeKey.wasPressedThisFrame && State == GameState.Death)
                {
                    SkipDeathRehearsal();
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
                            if (State == GameState.Death)
                            {
                                SkipDeathRehearsal();
                            }
                            else if (optionsOpen)
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
                else if (delta.y > OMTKConfig.SwipeDistanceThreshold && Mathf.Abs(delta.x) < OMTKConfig.SwipeDistanceThreshold && pointerDownTime < 0.48f)
                {
                    HandleSwipeUp();
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
            else if (optionsIndex == 3)
            {
                CycleVolume();
            }
            else
            {
                ResetProgress();
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
            string reset = "RESET PROGRESS";
            string[] rows = { narrator, haptics, radar, volume, reset };
            for (int i = 0; i < rows.Length; i++)
            {
                rows[i] = (i == optionsIndex ? "> " : "  ") + rows[i];
            }

            return rows[0] + "\n" + rows[1] + "\n" + rows[2] + "\n" + rows[3] + "\n" + rows[4] + "\nTAP: CHANGE   SWIPE: NEXT";
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

            if (optionsIndex == 3)
            {
                return "Volume " + Mathf.RoundToInt(masterVolume * 100f) + " percent.";
            }

            return "Reset progress.";
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
                TryDeathRehearsalTap();
            }
            else if (State == GameState.Finale)
            {
                TryFinaleParry();
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
            else if (State == GameState.Death)
            {
                return;
            }
            else if (State == GameState.Finale)
            {
                TryFinaleSwipe();
            }
            else if (State == GameState.Victory && StateTime > 0.6f)
            {
                StartRun();
            }
        }

        private void HandleSwipeUp()
        {
            if (State == GameState.Death)
            {
                TryAcceptBargainBySwipe();
            }
        }

        private void SetState(GameState state)
        {
            CancelTimeDip();
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
                bool winding = finaleThreatActive || (finaleBeat == 3 && !finaleFinalDefended);
                ShowRadarBlip(0, finaleTrueBearing, 4f, winding, new Color(1f, 0.58f, 0.32f), false);
            }
            else if (State == GameState.Death && fatalClip != null)
            {
                ShowRadarBlip(0, fatalAzimuth, fatalPosition.magnitude, deathCueActive, new Color(1f, 0.28f, 0.22f), false);
            }

            UpdateRadarAudio(deltaTime);
        }

        private void SetRadarVisible(bool visible)
        {
            if (radarDial == null)
            {
                return;
            }

            bool changed = radarVisible != visible;
            radarVisible = visible;
            if (changed)
            {
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
            }

            if (!visible && radarBlipRoots != null)
            {
                for (int i = 0; i < radarBlipRoots.Length; i++)
                {
                    if (radarBlipRoots[i] != null)
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

            if (Audio.IsSpeechActive)
            {
                radarPingTimer = 0.4f;
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
            Deaths = Mathf.Max(0, PlayerPrefs.GetInt(DeathsKey, 0));
            BestWave = Mathf.Clamp(PlayerPrefs.GetInt(BestWaveKey, 0), 0, 3);
            CodexMask = PlayerPrefs.GetInt(CodexKey, 0) & 7;
            LearnedMask = PlayerPrefs.GetInt(LearnedKey, 0) & 7;
            RunEchoMask = 0;
            ConductorResonance = 0;
            ironShieldAvailable = false;
            PlayerPrefs.DeleteKey(EchoKey);
            RecalculateMaxHp();
        }

        private void SaveProgress()
        {
            PlayerPrefs.SetInt(DeathsKey, Deaths);
            PlayerPrefs.SetInt(BestWaveKey, BestWave);
            PlayerPrefs.SetInt(CodexKey, CodexMask);
            PlayerPrefs.SetInt(LearnedKey, LearnedMask);
            PlayerPrefs.Save();
        }

        private void ResetProgress()
        {
            Deaths = 0;
            BestWave = 0;
            CodexMask = 0;
            LearnedMask = 0;
            ClearRunBargains();
            rehearsalBonusVariant = null;
            rehearsalBonusAttempt = 0;
            PlayerPrefs.DeleteKey(EchoKey);
            PlayerPrefs.DeleteKey(CodexKey);
            PlayerPrefs.DeleteKey(LearnedKey);
            PlayerPrefs.DeleteKey(BestWaveKey);
            PlayerPrefs.DeleteKey(DeathsKey);
            PlayerPrefs.Save();
            ShowSubtitle("PROGRESS RESET", 1.4f);
            Audio.Play2D(Audio.RecenterClip, 0.7f, 0.8f);
            Audio.Speak("Progress reset.");
        }

        private EchoId? UnlockNextEcho()
        {
            return null;
        }

        private void RecalculateMaxHp()
        {
            MaxHp = OMTKConfig.BaseMaxHp;
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
