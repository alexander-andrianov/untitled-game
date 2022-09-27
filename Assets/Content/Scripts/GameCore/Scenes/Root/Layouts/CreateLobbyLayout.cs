using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Content.Scripts.GameCore.Base;
using Content.Scripts.GameCore.Base.Interfaces;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Content.Scripts.GameCore.Scenes.Root.Layouts
{
    public class CreateLobbyLayout : LayoutBase, ILayout
    {
        [Header("BUTTONS")]
        [SerializeField] 
        private Button createButton;

        private readonly CompositeDisposable disposables = new CompositeDisposable();

        private readonly Subject<Unit> onCreate = new Subject<Unit>();
        
        private Transform buttonsLayout;

        public IObservable<Unit> OnCreate => onCreate;

        internal override void Initialize()
        {
            buttonsLayout = transform.GetChild(0);
            
            createButton.OnClickAsObservable().Subscribe(HandleCreate).AddTo(disposables);
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
            createButton.interactable = value;
        }

        private void HandleCreate(Unit unit)
        {
            onCreate.OnNext(unit);
        }
    }
}