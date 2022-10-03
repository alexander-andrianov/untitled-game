using Content.Scripts.GameCore.Scenes.Authentication.View;
using UnityEngine;

namespace Content.Scripts.GameCore.Scenes.Authentication.Entry
{
    public class AuthenticationEntry : MonoBehaviour
    {
        [Header("VIEW")] 
        [SerializeField] 
        private AuthenticationViewController authenticationViewController;
        
        private async void Start()
        {
            await authenticationViewController.Initialize();
        }
    }
}