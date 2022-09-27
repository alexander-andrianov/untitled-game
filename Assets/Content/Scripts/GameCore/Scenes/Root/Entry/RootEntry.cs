using System;
using System.Threading.Tasks;
using Content.Scripts.GameCore.Scenes.Root.View;
using UnityEngine;

namespace Content.Scripts.GameCore.Scenes.Root.Entry
{
    public class RootEntry : MonoBehaviour
    {
        private const float InitializationDelay = 1f;
        
        [Header("VIEW")] 
        [SerializeField] 
        private RootViewController rootViewController;
        
        private async void Start()
        {
            await Task.Delay(TimeSpan.FromSeconds(InitializationDelay));
            await rootViewController.Initialize();
        }
    }
}
