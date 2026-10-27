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

        private readonly List<OMTKEnemy> enemies = new List<OMTKEnemy>();
        private readonly float[] audioSamples = new float[256];
        private Camera mainCamera;
        private Image ring;
        private Text statusText;
        private Text titleText;
        private Text subtitleText;
        private Text promptText;
        private Font uiFont;
        private OMTKEnemy token;
        private float windupCooldown;
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
        private Vector2 pointerStart;
        private Vector2 pointerLast;
        private float pointerDownTime;
        private float pointerTravel;
        private bool applicationPaused;
        private float resumeTimeScale = 1f;

        private const string DeathsKey = "OMTK_Deaths";
        private const string BestWaveKey = "OMTK_BestWave";
        private const string EchoKey = "OMTK_EchoMask";

        private void Awake()
        {
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Screen.orientation = ScreenOrientation.Portrait;
            LoadProgress();
            CreateCamera();
            Audio = new OMTKAudio();
            Audio.Init(gameObject);
            CreateUI();
            SetState(GameState.Title);
        }

        private void Start()
        {
            ShowSubtitle("Turn your head.\nTap to parry. Swipe to answer.", 999f);
            promptText.text = "TAP TO BEGIN";
            Audio.Speak("One more time, knight.");
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
                resumeTimeScale = Time.timeScale;
                Time.timeScale = 0f;
            }
            else
            {
                Time.timeScale = resumeTimeScale;
            }
        }

        private void Update()
        {
            float deltaTime = Mathf.Min(Time.deltaTime, 0.05f);
            Audio.Tick();
            StateTime += deltaTime;
            UpdateInput(deltaTime);

            if (State == GameState.Intro && StateTime >= introDuration)
            {
                BeginWave(1);
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
            windupCooldown = 1.15f;
            Hp--;
            damageFlash = 1f;
            Audio.Play2D(Audio.HitClip, 0.95f, Random.Range(0.92f, 1.04f));
            ShowSubtitle("It found you.", 1.05f);
            promptText.text = "LISTEN";

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
                    }
                }
            }
        }

        private void StartRun()
        {
            Attempt = 1;
            Wave = 1;
            Hp = MaxHp;
            ClearEnemies();
            Audio.Unduck();
            Audio.PlayMusic();
            Audio.StopConductor();
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
            ClearEnemies();
            Audio.Unduck();
            Audio.PlayMusic();
            Audio.StopConductor();
            introDuration = 1.65f;
            SetState(GameState.Intro);
            ShowSubtitle("Again. Listen.", 1.6f);
            promptText.text = "LISTEN";
            Audio.Speak("Again.");
        }

        private void BeginWave(int wave)
        {
            Wave = wave;
            Hp = Mathf.Min(MaxHp, Hp + (wave > 1 ? 1 : 0));
            ClearEnemies();
            windupCooldown = 1.65f;
            SetState(GameState.Playing);

            if (wave == 1)
            {
                Spawn(EnemyVariant.Stalker, 62f);
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
                ShowSubtitle("Now. Swipe.", 1.4f);
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
            damageFlash = 1f;
            Audio.StopVoice();
            Audio.PlayFatal(killer.WindupClip, killer.Position);
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

            if (token != null && token.IsWindingUp)
            {
                bool perfect = token.WindupRemaining <= PerfectWindow;
                token.Parry(perfect);
                token = null;
                windupCooldown = 0.9f;

                if (perfect)
                {
                    parryFlash = 1f;
                    Audio.Play2D(Audio.PerfectClip, 0.9f);
                    ShowSubtitle("Now. Swipe.", 1.15f);
                    promptText.text = "SWIPE";
                }
                else
                {
                    Audio.Play2D(Audio.ParryClip, 0.78f);
                    ShowSubtitle("Blocked. Wait for the opening.", 1.2f);
                    promptText.text = "LISTEN";
                }

                return;
            }

            OMTKEnemy staggered = FindStaggered();
            if (staggered != null)
            {
                KillEnemy(staggered);
                return;
            }

            Audio.Play2D(Audio.WhiffClip, 0.4f);
            ShowSubtitle("Nothing there.", 0.65f);
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

        private void KillEnemy(OMTKEnemy enemy)
        {
            if (!enemy.Kill())
            {
                return;
            }

            if (token == enemy)
            {
                token = null;
            }

            windupCooldown = 0.82f;
            parryFlash = 1f;
            Audio.Play2D(Audio.SlashClip, 0.9f, Random.Range(0.96f, 1.06f));
            ShowSubtitle("One less echo.", 0.95f);
            promptText.text = "LISTEN";
            BestWave = Mathf.Max(BestWave, Wave);
            SaveProgress();
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

        private void Victory()
        {
            SetState(GameState.Victory);
            BestWave = 3;
            SaveProgress();
            Audio.StopConductor();
            Audio.Unduck();
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
                    PlayerYaw += deltaX * 0.18f;
                    pointerTravel += Mathf.Abs(deltaX);
                }

                pointerLast = position;
            }

            if (up && pointerHeld)
            {
                pointerHeld = false;
                Vector2 delta = position - pointerStart;
                if (pointerTravel < 18f && delta.magnitude < 45f && pointerDownTime < 0.42f)
                {
                    HandleTap();
                }
                else if (Mathf.Abs(delta.x) > 90f && Mathf.Abs(delta.y) < 90f && pointerDownTime < 0.48f)
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

        private void HandleTap()
        {
            if (State == GameState.Title)
            {
                StartRun();
            }
            else if (State == GameState.Intro && StateTime > 0.55f)
            {
                BeginWave(1);
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
                StartRun();
            }
            else if (State == GameState.Intro && StateTime > 0.55f)
            {
                BeginWave(1);
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
                ShowSubtitle("Turn toward the voice.", 1.2f);
                promptText.text = "TURN";
                return;
            }

            Victory();
        }

        private void SetState(GameState state)
        {
            State = state;
            StateTime = 0f;

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

        private void UpdateHud()
        {
            if (statusText == null)
            {
                return;
            }

            if (State == GameState.Title)
            {
                statusText.text = "HEADPHONES ON";
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

            statusText = CreateText("Status", canvasObject.transform, 28, new Color(0.78f, 0.84f, 0.9f, 0.86f), TextAnchor.UpperLeft);
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

            promptText = CreateText("Prompt", canvasObject.transform, 32, new Color(0.9f, 0.95f, 1f, 0.9f), TextAnchor.MiddleCenter);
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
