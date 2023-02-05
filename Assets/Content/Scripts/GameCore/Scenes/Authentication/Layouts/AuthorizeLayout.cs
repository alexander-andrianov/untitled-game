using Content.Scripts.GameCore.Base;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Content.Scripts.GameCore.Scenes.Authentication.Layouts
{
    public class AuthorizeLayout : LayoutBase
    {
        [Header("BUTTONS")]
        [SerializeField]
        private Button authorizeButton;

        private readonly CompositeDisposable disposables = new CompositeDisposable();

        private readonly Subject<Unit> onAuthorize = new Subject<Unit>();

        private Transform buttonsLayout;

        public IObservable<Unit> OnAuthorize => onAuthorize;

        private void OnDestroy()
        {
            disposables.Dispose();
        }

        public override async Task SetLayoutVisible(bool value)
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

        public override void SetButtonsInteractable(bool value)
        {
            authorizeButton.interactable = value;
        }

        internal override void Initialize()
        {
            buttonsLayout = transform.GetChild(0);

            authorizeButton.OnClickAsObservable().Subscribe(HandleAuthorize).AddTo(disposables);
        }

        protected override void OnLayoutShowing()
        {
            authorizeButton.Select();
        }

        private void HandleAuthorize(Unit unit)
        {
            onAuthorize.OnNext(unit);
        }
    }
}