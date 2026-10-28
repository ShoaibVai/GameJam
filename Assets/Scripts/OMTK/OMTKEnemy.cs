using UnityEngine;

namespace OMTK
{
    public enum EnemyState
    {
        Approach,
        Windup,
        Recover,
        Stagger,
        Dead
    }

    public enum ParryGrade
    {
        Perfect,
        Standard,
        Glancing
    }

    public class OMTKEnemy
    {
        public EnemyVariant Variant { get; private set; }
        public EnemyState State { get; private set; }
        public float Azimuth { get; private set; }
        public float Distance { get; private set; }
        public float WindupRemaining { get; private set; }
        public float StaggerRemaining { get; private set; }
        public float WindupDuration { get; private set; }
        public AudioClip WindupClip { get; private set; }
        public AudioClip PresenceClip { get; private set; }

        public bool IsAlive
        {
            get { return State != EnemyState.Dead; }
        }

        public bool IsWindingUp
        {
            get { return State == EnemyState.Windup; }
        }

        public bool IsStaggered
        {
            get { return State == EnemyState.Stagger; }
        }

        public bool IsResonanceLocked { get; private set; }
        public bool TrainingCuePlayed { get; private set; }

        public float Attention
        {
            get { return attention; }
        }

        public float PresenceTempo
        {
            get
            {
                if (Variant != EnemyVariant.Charger)
                {
                    return 1f;
                }

                float near = OMTKConfig.ChargerNearDistance;
                float far = OMTKConfig.ChargerFarDistance;
                float distance = Mathf.Clamp(Distance, near, far);
                float closeness = (far - distance) / (far - near);
                return Mathf.Lerp(OMTKConfig.ChargerFarTempo, OMTKConfig.ChargerNearTempo, closeness);
            }
        }

        public float DecoyAzimuth
        {
            get { return Mathf.Repeat(Azimuth + 90f * phantomSide, 360f); }
        }

        public bool DecoyActive
        {
            get { return phantom != null && phantom.isPlaying; }
        }

        public float LockProgress
        {
            get { return lockProgress; }
        }

        public bool AtTarget
        {
            get { return State == EnemyState.Approach && Distance <= 3.05f; }
        }

        public Vector3 Position
        {
            get { return Quaternion.Euler(0f, Azimuth, 0f) * Vector3.forward * Distance; }
        }

        private readonly OMTKGame game;
        private readonly OMTKAudio audio;
        private readonly GameObject root;
        private readonly AudioSource source;
        private readonly AudioSource presence;
        private readonly AudioSource phantom;
        private readonly AudioLowPassFilter sourceFilter;
        private readonly AudioLowPassFilter presenceFilter;
        private readonly AudioLowPassFilter phantomFilter;
        private float baseAzimuth;
        private float phase;
        private float moveSpeed;
        private float baseMoveSpeed;
        private float targetDistance;
        private float recoverRemaining;
        private bool foresightCue;
        private bool runForesightCue;
        private float presenceTarget;
        private float lockProgress;
        private float chargerPulseTimer;
        private float attention = 1f;
        private float attentionTarget = 1f;
        private int phantomSide;
        private bool phantomDecoy;
        private const float ApproachDistance = 2.35f;

        public OMTKEnemy(OMTKGame owner, OMTKAudio audioSystem, EnemyVariant variant, float azimuth, int index)
        {
            game = owner;
            audio = audioSystem;
            Variant = variant;
            baseAzimuth = azimuth;
            Azimuth = azimuth;
            Distance = 9.2f + index * 1.1f;
            phase = index * 1.73f;
            targetDistance = ApproachDistance;
            moveSpeed = variant == EnemyVariant.Charger ? 2.35f : variant == EnemyVariant.Conjurer ? 0.95f : 1.25f;
            baseMoveSpeed = moveSpeed;
            phantomSide = index % 2 == 0 ? 1 : -1;
            WindupDuration = (variant == EnemyVariant.Charger ? 0.65f : variant == EnemyVariant.Conjurer ? 0.95f : 0.85f) * owner.WindupScaleFor(variant);
            WindupClip = audio.Windup(variant);
            PresenceClip = audio.Presence(variant);

            root = new GameObject("Enemy_" + variant);
            root.transform.SetParent(owner.transform, false);
            source = CreateSource(root, "Attack", 1.2f, 12f);
            presence = CreateSource(root, "Presence", 1.5f, 10f);
            phantom = CreateSource(root, "Phantom", 1.5f, 11f);
            sourceFilter = source.gameObject.AddComponent<AudioLowPassFilter>();
            presenceFilter = presence.gameObject.AddComponent<AudioLowPassFilter>();
            phantomFilter = phantom.gameObject.AddComponent<AudioLowPassFilter>();
            sourceFilter.cutoffFrequency = 5000f;
            presenceFilter.cutoffFrequency = 5000f;
            phantomFilter.cutoffFrequency = 5000f;
            sourceFilter.lowpassResonanceQ = 0.7f;
            presenceFilter.lowpassResonanceQ = 0.7f;
            phantomFilter.lowpassResonanceQ = 0.7f;

            presence.clip = variant == EnemyVariant.Charger ? audio.ChargerPulseClip : PresenceClip;
            presence.loop = variant != EnemyVariant.Charger;
            presence.volume = 0.24f;
            if (variant != EnemyVariant.Charger)
            {
                presence.Play();
            }

            presenceTarget = 0.24f;
            if (variant == EnemyVariant.Conjurer)
            {
                StartPhantomApproach();
            }

            UpdateSpatial(0f);
        }

        public void Tick(float deltaTime, float playerYaw, float perfectWindow)
        {
            if (!IsAlive)
            {
                return;
            }

            if (State == EnemyState.Approach)
            {
                if (Variant == EnemyVariant.Stalker)
                {
                    Azimuth = baseAzimuth + Mathf.Sin(Time.time * 0.55f + phase) * OMTKConfig.StalkerOscillation;
                }
                else if (Variant == EnemyVariant.Conjurer && !phantomDecoy)
                {
                    Azimuth = baseAzimuth + Mathf.Sin(Time.time * 0.31f + phase) * 13f;
                }

                Distance = Mathf.MoveTowards(Distance, targetDistance, moveSpeed * deltaTime);
            }
            else if (State == EnemyState.Recover)
            {
                Distance = Mathf.MoveTowards(Distance, targetDistance, moveSpeed * 1.9f * deltaTime);
                recoverRemaining -= deltaTime;
                if (recoverRemaining <= 0f)
                {
                    State = EnemyState.Approach;
                    targetDistance = ApproachDistance;
                }
            }
            else if (State == EnemyState.Windup)
            {
                WindupRemaining -= deltaTime;
                if (!runForesightCue && WindupRemaining <= 0.4f && game.HasEcho(EchoId.Foresight))
                {
                    runForesightCue = true;
                    audio.PlayAt(audio.ForesightClip, Position, 0.5f, 1.3f);
                }

                if (!foresightCue && WindupRemaining <= perfectWindow && game.TryTrainingChime(Variant, Position))
                {
                    foresightCue = true;
                    TrainingCuePlayed = true;
                }

                if (WindupRemaining <= 0f)
                {
                    source.Stop();
                    State = EnemyState.Recover;
                    recoverRemaining = 0.62f;
                    targetDistance = 7.4f;
                    presenceTarget = 0.2f;
                    game.EnemyAttack(this);
                }
            }
            else if (State == EnemyState.Stagger)
            {
                StaggerRemaining -= deltaTime;
                if (StaggerRemaining <= 0f)
                {
                    State = EnemyState.Recover;
                    recoverRemaining = 0.45f;
                    targetDistance = 6.4f;
                    presenceTarget = 0.24f;
                }
            }

            attention = Mathf.MoveTowards(attention, attentionTarget, deltaTime * 2.8f);
            UpdateChargerPulse(deltaTime);
            UpdatePhantom(deltaTime);
            UpdateResonanceLock(deltaTime, playerYaw);
            presence.volume = Mathf.MoveTowards(presence.volume, presenceTarget, deltaTime * 2.4f);
            UpdateSpatial(playerYaw);
        }

        public void SetAttention(float value)
        {
            attentionTarget = Mathf.Clamp01(value);
        }

        public void SetApproachScale(float scale)
        {
            moveSpeed = baseMoveSpeed * Mathf.Max(0.5f, scale);
        }

        private void UpdateChargerPulse(float deltaTime)
        {
            if (Variant != EnemyVariant.Charger)
            {
                return;
            }

            chargerPulseTimer -= deltaTime;
            if (chargerPulseTimer <= 0f)
            {
                chargerPulseTimer = 1f / Mathf.Max(0.1f, PresenceTempo);
                if (presence != null && presence.clip != null)
                {
                    presence.Play();
                }
            }
        }

        private void UpdatePhantom(float deltaTime)
        {
            if (Variant != EnemyVariant.Conjurer || phantom == null)
            {
                return;
            }

            if (phantomDecoy && !phantom.isPlaying)
            {
                StopPhantom();
            }

            if (State == EnemyState.Approach && !phantomDecoy && !phantom.isPlaying)
            {
                StartPhantomApproach();
            }
            else if (State != EnemyState.Approach && !phantomDecoy)
            {
                phantom.Stop();
            }

            if (phantom.isPlaying)
            {
                phantom.transform.position = Quaternion.Euler(0f, DecoyAzimuth, 0f) * Vector3.forward * Distance;
                phantomFilter.cutoffFrequency = Mathf.Lerp(1100f, 5000f, attention);
            }
        }

        private void StartPhantomApproach()
        {
            if (phantom == null)
            {
                return;
            }

            phantomDecoy = false;
            phantom.clip = audio.DecoyApproachClip;
            phantom.loop = true;
            phantom.volume = 0.13f;
            if (!phantom.isPlaying)
            {
                phantom.Play();
            }
        }

        public void CancelDecoy()
        {
            StopPhantom();
        }

        public void Interrupt()
        {
            if (State != EnemyState.Windup)
            {
                return;
            }

            source.Stop();
            StopPhantom();
            State = EnemyState.Recover;
            recoverRemaining = 0.55f;
            targetDistance = 7.4f;
            presenceTarget = 0.2f;
        }

        private void StopPhantom()
        {
            phantomDecoy = false;
            if (phantom != null)
            {
                phantom.Stop();
            }
        }

        private void UpdateResonanceLock(float deltaTime, float playerYaw)
        {
            float angle = Mathf.Abs(Mathf.DeltaAngle(playerYaw, Azimuth));
            if (angle <= OMTKConfig.StandardParryAngle)
            {
                if (IsResonanceLocked)
                {
                    return;
                }

                lockProgress += deltaTime;
                if (lockProgress >= OMTKConfig.ResonanceLockDuration)
                {
                    IsResonanceLocked = true;
                    audio.PlayAt(audio.Lock(Variant), Position, 0.52f);
                }
            }
            else
            {
                IsResonanceLocked = false;
                lockProgress = 0f;
            }
        }

        public void PlaceAtDistance(float distance)
        {
            Distance = Mathf.Max(0.6f, distance);
        }

        public bool BeginScriptedWindup(float duration)
        {
            if (!IsAlive)
            {
                return false;
            }

            StopPhantom();
            State = EnemyState.Windup;
            WindupDuration = duration;
            WindupRemaining = duration;
            foresightCue = true;
            runForesightCue = true;
            TrainingCuePlayed = false;
            source.clip = WindupClip;
            source.pitch = 1f;
            source.volume = 0.95f;
            source.Play();
            presenceTarget = 0.07f;
            return true;
        }

        public bool BeginDecoyWindup()
        {
            if (Variant != EnemyVariant.Conjurer || State != EnemyState.Approach || Distance > 3.15f || phantom == null)
            {
                return false;
            }

            phantomDecoy = true;
            phantom.Stop();
            phantom.clip = audio.DecoyWindupClip;
            phantom.loop = false;
            phantom.volume = 0.52f;
            phantom.transform.position = Quaternion.Euler(0f, DecoyAzimuth, 0f) * Vector3.forward * Distance;
            phantom.Play();
            return true;
        }

        public bool BeginWindup()
        {
            if (State != EnemyState.Approach || Distance > 3.15f)
            {
                return false;
            }

            StopPhantom();
            State = EnemyState.Windup;
            WindupRemaining = WindupDuration;
            foresightCue = false;
            runForesightCue = false;
            TrainingCuePlayed = false;
            source.clip = WindupClip;
            source.pitch = 1f;
            source.volume = 0.95f;
            source.Play();
            presenceTarget = 0.07f;
            return true;
        }

        public bool Parry(ParryGrade grade)
        {
            if (State != EnemyState.Windup)
            {
                return false;
            }

            source.Stop();
            if (grade == ParryGrade.Perfect)
            {
                State = EnemyState.Stagger;
                StaggerRemaining = OMTKConfig.PerfectStaggerDuration;
                presenceTarget = 0.12f;
                audio.PlayAt(audio.StaggerClip, Position, 0.75f);
            }
            else if (grade == ParryGrade.Glancing)
            {
                if (Variant == EnemyVariant.Charger)
                {
                    State = EnemyState.Approach;
                    Azimuth = Mathf.Repeat(Azimuth + 180f, 360f);
                    baseAzimuth = Azimuth;
                    Distance = OMTKConfig.ChargerOvershootDistance;
                    targetDistance = ApproachDistance;
                    moveSpeed = baseMoveSpeed * 1.15f;
                    IsResonanceLocked = false;
                    lockProgress = 0f;
                    chargerPulseTimer = 0f;
                    presenceTarget = 0.2f;
                    audio.PlayAt(audio.GlanceClip, Position, 0.72f);
                }
                else
                {
                    State = EnemyState.Recover;
                    recoverRemaining = 0.34f;
                    targetDistance = 7.6f;
                    presenceTarget = 0.2f;
                    audio.PlayAt(audio.GlanceClip, Position, 0.72f);
                }
            }
            else
            {
                State = EnemyState.Recover;
                recoverRemaining = 0.42f;
                targetDistance = 6.6f;
                presenceTarget = 0.22f;
            }

            return true;
        }

        public bool ShoveAway()
        {
            if (State != EnemyState.Stagger)
            {
                return false;
            }

            State = EnemyState.Recover;
            recoverRemaining = 0.5f;
            Distance = 7.4f;
            targetDistance = 7.4f;
            presenceTarget = 0.2f;
            return true;
        }

        public bool Kill()
        {
            if (State != EnemyState.Stagger)
            {
                return false;
            }

            State = EnemyState.Dead;
            source.Stop();
            presence.Stop();
            StopPhantom();
            audio.PlayAt(audio.DeathClip, Position, 0.86f);
            Object.Destroy(root, 1.35f);
            return true;
        }

        public void Stop()
        {
            if (source != null)
            {
                source.Stop();
            }

            if (presence != null)
            {
                presence.Stop();
            }

            StopPhantom();
        }

        public void Destroy()
        {
            if (root != null)
            {
                Object.Destroy(root);
            }
        }

        private void UpdateSpatial(float playerYaw)
        {
            root.transform.position = Position;
            float relative = Mathf.Abs(Mathf.DeltaAngle(playerYaw, Azimuth));
            float front = 1f - Mathf.Clamp01(relative / 180f);
            float cutoff = Mathf.Lerp(720f, 5000f, front * front);
            cutoff *= Mathf.Lerp(OMTKConfig.UnattendedCutoffScale, 1f, attention);
            sourceFilter.cutoffFrequency = cutoff;
            presenceFilter.cutoffFrequency = cutoff;
        }

        private AudioSource CreateSource(GameObject parent, string name, float minDistance, float maxDistance)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            AudioSource created = go.AddComponent<AudioSource>();
            created.playOnAwake = false;
            created.spatialBlend = 1f;
            created.rolloffMode = AudioRolloffMode.Linear;
            created.minDistance = minDistance;
            created.maxDistance = maxDistance;
            created.dopplerLevel = 0f;
            created.spread = 0f;
            return created;
        }
    }
}
