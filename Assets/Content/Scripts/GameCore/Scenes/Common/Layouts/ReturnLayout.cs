using System;
using System.Threading.Tasks;
using Content.Scripts.GameCore.Base;
using Content.Scripts.GameCore.Base.Interfaces;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Content.Scripts.GameCore.Scenes.Common.Layouts
{
    public class ReturnLayout : LayoutBase, ILayout
    {
        [Header("BUTTONS")]
        [SerializeField]
        private Button backButton;

        private readonly CompositeDisposable disposables = new CompositeDisposable();

        private readonly Subject<Unit> onBack = new Subject<Unit>();
        
        private Transform buttonsLayout;

        public IObservable<Unit> OnBack => onBack;

        internal override void Initialize()
        {
            buttonsLayout = transform.GetChild(0);
            
            backButton.OnClickAsObservable().Subscribe(HandleBack).AddTo(disposables);
        }

        private void OnDestroy()
        {
            disposables.Dispose();
        }
        
        public async Task SetLayoutVisible(bool value)
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
        
        public void SetButtonsInteractable(bool value) {
            backButton.interactable = value;
        }

        private void HandleBack(Unit unit)
        {
            onBack.OnNext(unit);
        }
    }
}