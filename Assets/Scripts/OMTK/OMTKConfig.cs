using UnityEngine;

namespace OMTK
{
    public enum EnemyVariant
    {
        Stalker,
        Charger,
        Conjurer
    }

    public enum EchoId
    {
        Foresight,
        Grace,
        Iron,
        Stillness
    }

    public static class OMTKConfig
    {
        public static bool RadarAssist = false;

        public const string ProductName = "One More Time, Knight";
        public const string ApplicationIdentifier = "com.shoaib.onemoretimeknight";
        public const int SampleRate = 44100;
        public const float PerfectWindow = 0.14f;
        public const float GracePerfectWindow = 0.22f;
        public const float PerfectParryAngle = 18f;
        public const float GracePerfectParryAngle = 25f;
        public const float StandardParryAngle = 35f;
        public const float GlancingDeflectAngle = 40f;
        public const float StandardParryWindow = 0.35f;
        public const float StandardParryCooldownScale = 0.7f;
        public const float ResonanceLockDuration = 0.3f;
        public const float GuardLockoutDuration = 0.45f;
        public const float TinnitusFrequency = 900f;
        public const float TinnitusDuration = 0.8f;
        public const float PerfectStaggerDuration = 1.2f;
        public const float TouchDeadZone = 8f;
        public const float SwipeDistanceThreshold = 90f;
        public const float SwipeVelocityThreshold = 800f;
        public const float CalibrationAzimuth = 150f;
        public const float CalibrationAlignAngle = 15f;
        public const float CalibrationToneDistance = 3.2f;
        public const float CalibrationFallbackDuration = 10.5f;
        public const float CalibrationNudgeInterval = 3.4f;
        public const float TutorialFirstDeathDelay = 3f;
        public const float TutorialFirstDeathWindup = 0.85f;
        public const int BaseMaxHp = 3;
        public const float RunIntroDuration = 3.4f;
        public const float FinaleTurnWindow = 35f;
        public const float DeathNoteTime = 1.05f;
        public const float DeathCueFirstTime = 2.1f;
        public const float DeathCueInterval = 1.8f;
        public const float DeathCuePitch = 0.62f;
        public const float DeathCueLowPass = 1600f;
        public const float DeathRehearsalAngle = 25f;
        public const float DeathCueWindowDelay = 0.35f;
        public const float DeathCueWindowDuration = 0.35f;
        public const float DeathFallbackDuration = 4.8f;
        public const float DeathSuccessDelay = 0.55f;
        public const float RehearsalWindupScale = 0.85f;
        public const int TrainingChimeCharges = 3;
        public const float FinaleDriftStart = 1.4f;
        public const float FinaleDriftDuration = 2.6f;
        public const float FinaleThreatDeadline = 4f;
        public const float FinaleThreatLead = 0.9f;
        public const float FinaleThreatWindowDelay = 1.25f;
        public const float FinaleThreatWindowDuration = 0.35f;
        public const float FinaleFinalSwipeAngle = 20f;
        public const float FinaleOffAxisAngle = 45f;
        public const float FinaleRestartDelay = 0.7f;

        public const float StalkerOscillation = 32f;
        public const float ChargerFarDistance = 10f;
        public const float ChargerNearDistance = 2.35f;
        public const float ChargerFarTempo = 1f;
        public const float ChargerNearTempo = 5.5f;
        public const float ChargerOvershootDistance = 4.5f;
        public const float ConjurerDecoyLead = 0.4f;
        public const float OffbeatWindupDelay = 0.55f;
        public const float OffbeatTimeScale = 0.65f;
        public const float OffbeatTimeDipDuration = 0.4f;
        public const float UnattendedCutoffScale = 0.45f;
        public const float AttackCooldown = 1.25f;
        public const float WaveStartCooldown = 1.8f;
        public const float MinEnemySeparation = 85f;
        public const float StalkerBearingJitter = 32f;
        public const float ChargerBearingJitter = 40f;
        public const float ConjurerBearingJitter = 34f;

        public static readonly EchoId[] AllEchoes =
        {
            EchoId.Foresight,
            EchoId.Grace,
            EchoId.Iron,
            EchoId.Stillness
        };

        public static string EchoName(EchoId id)
        {
            switch (id)
            {
                case EchoId.Foresight:
                    return "Echo of Foresight";
                case EchoId.Grace:
                    return "Echo of Grace";
                case EchoId.Iron:
                    return "Echo of Iron";
                case EchoId.Stillness:
                    return "Echo of Stillness";
            }

            return "Echo";
        }

        public static string EchoHint(EchoId id)
        {
            switch (id)
            {
                case EchoId.Foresight:
                    return "A chime marks the perfect window.";
                case EchoId.Grace:
                    return "The opening is wider.";
                case EchoId.Iron:
                    return "One more heart.";
                case EchoId.Stillness:
                    return "The enemy gathers itself more slowly.";
            }

            return string.Empty;
        }

        public static int EchoBit(EchoId id)
        {
            return 1 << (int)id;
        }

        public static int CodexBit(EnemyVariant variant)
        {
            return 1 << (int)variant;
        }

        public static string CodexName(EnemyVariant variant)
        {
            switch (variant)
            {
                case EnemyVariant.Stalker:
                    return "Stalker";
                case EnemyVariant.Charger:
                    return "Charger";
                case EnemyVariant.Conjurer:
                    return "Conjurer";
            }

            return "Echo";
        }

        public static int RequiredDeaths(EchoId id)
        {
            switch (id)
            {
                case EchoId.Foresight:
                    return 1;
                case EchoId.Grace:
                    return 2;
                case EchoId.Iron:
                    return 3;
                case EchoId.Stillness:
                    return 5;
            }

            return 999;
        }
    }
}
