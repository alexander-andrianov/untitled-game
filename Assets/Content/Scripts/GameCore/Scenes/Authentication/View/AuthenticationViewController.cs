using System.Threading.Tasks;
using Content.Scripts.GameCore.Base.Interfaces;
using Content.Scripts.GameCore.Scenes.Authentication.Layouts;
using Content.Scripts.GameCore.Scenes.Common.Tools;
using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Content.Scripts.GameCore.Scenes.Authentication.View
{
    public class AuthenticationViewController : MonoBehaviour
    {
        private const string LoaderText = "Loading...";
        private const string RootSceneName = "Root";
        
        [Header("LAYOUTS")]
        [SerializeField] 
        private AuthorizeLayout authorizeLayout;
        
        private readonly CompositeDisposable disposables = new CompositeDisposable();
        
        private void OnDestroy()
        {
            disposables.Dispose();
        }

        public async Task Initialize()
        {
            InitializeAuthorizeLayout();
            
            authorizeLayout.OnAuthorize.Subscribe(HandleAuthorize).AddTo(disposables);
            await ShowLayoutView(authorizeLayout);
        }
        
        private void InitializeAuthorizeLayout()
        {
            authorizeLayout.Initialize();
        }

        private async Task ShowLayoutView(ILayout layout)
        {
            await layout.SetLayoutVisible(true);
        }
        
        private async void HandleAuthorize(Unit unit)
        {
            using var loader = new SceneLoader();
            
            await Services.Authentication.Login();
            await loader.ShowLoader(LoaderText);
            
            await SceneManager.LoadSceneAsync(RootSceneName);
            await loader.HideLoader(LoaderText);
        }
    }
}
