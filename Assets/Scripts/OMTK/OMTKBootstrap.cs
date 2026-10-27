using UnityEngine;

namespace OMTK
{
    public static class OMTKBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (Object.FindAnyObjectByType<OMTKGame>() != null)
            {
                return;
            }

            GameObject root = new GameObject("One More Time, Knight");
            root.AddComponent<OMTKGame>();
        }
    }
}
