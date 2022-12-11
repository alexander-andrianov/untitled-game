using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Content.Scripts.GameCore.Base;
using Content.Scripts.GameCore.Base.Interfaces;
using Content.Scripts.GameCore.Data;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Content.Scripts.GameCore.Scenes.Root.Layouts
{
    public class LobbyLayout : LayoutBase, ILayout
    {
        [Header("TEXT")] 
        [SerializeField] private TextMeshProUGUI lobbyTitle;
        
        [Header("BUTTONS")] 
        [SerializeField] private Button readyButton;
        [SerializeField] private Button playButton;

        private readonly CompositeDisposable disposables = new CompositeDisposable();

        private readonly Subject<Unit> onReady = new Subject<Unit>();
        private readonly Subject<Unit> onPlay = new Subject<Unit>();

        private Transform buttonsLayout;
        
        public IObservable<Unit> OnReady => onReady;
        public IObservable<Unit> OnCreate => onPlay;

        internal override void Initialize()
        {
            buttonsLayout = transform.GetChild(0);

            readyButton.OnClickAsObservable().Subscribe(HandleReady).AddTo(disposables);
            playButton.OnClickAsObservable().Subscribe(HandlePlay).AddTo(disposables);
        }

        private void OnDestroy()
        {
            disposables.Dispose();
        }

        public void UpdateLobbyData(LobbyData data)
        {
            lobbyTitle.text = data.Name;
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

        public void SetButtonsInteractable(bool value)
        {
            readyButton.interactable = value;
            playButton.interactable = value;
        }
        
        private void HandleReady(Unit unit)
        {
            onReady.OnNext(unit);
        }

        private void HandlePlay(Unit unit)
        {
            onPlay.OnNext(unit);
        }
    }
}