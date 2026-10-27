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
