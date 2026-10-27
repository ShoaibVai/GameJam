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
        private readonly AudioLowPassFilter sourceFilter;
        private readonly AudioLowPassFilter presenceFilter;
        private float baseAzimuth;
        private float phase;
        private float moveSpeed;
        private float targetDistance;
        private float recoverRemaining;
        private bool foresightCue;
        private float presenceTarget;
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
            WindupDuration = (variant == EnemyVariant.Charger ? 0.65f : variant == EnemyVariant.Conjurer ? 0.95f : 0.85f) * owner.WindupScale;
            WindupClip = audio.Windup(variant);
            PresenceClip = audio.Presence(variant);

            root = new GameObject("Enemy_" + variant);
            root.transform.SetParent(owner.transform, false);
            source = CreateSource(root, "Attack", 1.2f, 12f);
            presence = CreateSource(root, "Presence", 1.5f, 10f);
            sourceFilter = source.gameObject.AddComponent<AudioLowPassFilter>();
            presenceFilter = presence.gameObject.AddComponent<AudioLowPassFilter>();
            sourceFilter.cutoffFrequency = 5000f;
            presenceFilter.cutoffFrequency = 5000f;
            sourceFilter.lowpassResonanceQ = 0.7f;
            presenceFilter.lowpassResonanceQ = 0.7f;

            presence.clip = PresenceClip;
            presence.loop = true;
            presence.volume = 0.24f;
            presence.Play();
            presenceTarget = 0.24f;
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
                    Azimuth = baseAzimuth + Mathf.Sin(Time.time * 0.55f + phase) * 24f;
                }
                else if (Variant == EnemyVariant.Conjurer)
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
                if (!foresightCue && game.HasEcho(EchoId.Foresight) && WindupRemaining <= perfectWindow)
                {
                    foresightCue = true;
                    audio.PlayAt(audio.ForesightClip, Position, 0.7f);
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

            presence.volume = Mathf.MoveTowards(presence.volume, presenceTarget, deltaTime * 2.4f);
            UpdateSpatial(playerYaw);
        }

        public bool BeginWindup()
        {
            if (State != EnemyState.Approach || Distance > 3.15f)
            {
                return false;
            }

            State = EnemyState.Windup;
            WindupRemaining = WindupDuration;
            foresightCue = false;
            source.clip = WindupClip;
            source.pitch = 1f;
            source.volume = 0.95f;
            source.Play();
            presenceTarget = 0.07f;
            return true;
        }

        public bool Parry(bool perfect)
        {
            if (State != EnemyState.Windup)
            {
                return false;
            }

            source.Stop();
            if (perfect)
            {
                State = EnemyState.Stagger;
                StaggerRemaining = 1.2f;
                presenceTarget = 0.12f;
                audio.PlayAt(audio.StaggerClip, Position, 0.75f);
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

        public bool Kill()
        {
            if (State != EnemyState.Stagger)
            {
                return false;
            }

            State = EnemyState.Dead;
            source.Stop();
            presence.Stop();
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
