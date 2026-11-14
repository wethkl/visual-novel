using UnityEngine;

namespace Verden
{
    public static class GameBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
#if UNITY_2023_1_OR_NEWER
            if (Object.FindFirstObjectByType<VNController>() != null) return;
#else
            if (Object.FindObjectOfType<VNController>() != null) return;
#endif
            GameObject host = new GameObject("VisualNovel");
            host.AddComponent<VNController>();
        }
    }
}
