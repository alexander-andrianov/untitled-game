using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Content.Scripts.GameCore.Base;
using Content.Scripts.GameCore.Base.Interfaces;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Content.Scripts.GameCore.Scenes.Authentication.Layouts
{
    public class AuthorizeLayout : LayoutBase, ILayout
    {
        [Header("BUTTONS")]
        [SerializeField]
        private Button authorizeButton;

        private readonly CompositeDisposable disposables = new CompositeDisposable();

        private readonly Subject<Unit> onAuthorize = new Subject<Unit>();
        
        private Transform buttonsLayout;

        public IObservable<Unit> OnAuthorize => onAuthorize;

        internal override void Initialize()
        {
            buttonsLayout = transform.GetChild(0);
            
            authorizeButton.OnClickAsObservable().Subscribe(HandleAuthorize).AddTo(disposables);
        }

        private void OnDestroy()
        {
            disposables.Dispose();
        }
        
        public async Task SetLayoutVisible(bool value)
        {
            try
            {
                if (value)
                {
                    await ShowLayout(buttonsLayout);
                }
                else
                {
                    await HideLayout(buttonsLayout);
                }
            }
            catch (Exception exception)
            {
                Trace.WriteLine(exception);
            }
        }
        
        public void SetButtonsInteractable(bool value) {
            authorizeButton.interactable = value;
        }

        private void HandleAuthorize(Unit unit)
        {
            onAuthorize.OnNext(unit);
        }
    }
}