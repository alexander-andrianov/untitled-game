using Unity.Netcode;
using UnityEngine;
using UnityEngine.AddressableAssets;

public static class Bootstrapper {
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize() {
        Addressables.InstantiateAsync("CanvasUtilities");
        Addressables.InstantiateAsync("NetworkingManager");
    }
}