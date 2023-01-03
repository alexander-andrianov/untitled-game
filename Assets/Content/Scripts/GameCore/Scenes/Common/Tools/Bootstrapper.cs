using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Content.Scripts.GameCore.Scenes.Common.Tools
{
    public static class Bootstrapper {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize() {
            Addressables.InstantiateAsync("CanvasUtilities");
            Addressables.InstantiateAsync("NetworkingManager");
            Addressables.InstantiateAsync("ChatManager");
        }
    }
}