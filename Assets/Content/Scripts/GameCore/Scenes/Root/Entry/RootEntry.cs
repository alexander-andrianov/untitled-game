using Content.Scripts.GameCore.Scenes.Root.Views;
using UnityEngine;

namespace Content.Scripts.GameCore.Scenes.Root.Entry
{
    public class RootEntry : MonoBehaviour
    {
        [Header("VIEW")] 
        [SerializeField] private RootViewController rootViewController;
        
        private async void Start()
        {
            await rootViewController.Initialize();
        }
    }
}
