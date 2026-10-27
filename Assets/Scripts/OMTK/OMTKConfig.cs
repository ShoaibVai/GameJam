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
        public const string ProductName = "One More Time, Knight";
        public const string ApplicationIdentifier = "com.shoaib.onemoretimeknight";
        public const int SampleRate = 44100;
        public const float PerfectWindow = 0.14f;
        public const float GracePerfectWindow = 0.22f;
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
