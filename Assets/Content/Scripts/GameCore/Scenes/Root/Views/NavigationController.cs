using Content.Scripts.GameCore.Scenes.Common.Enums;
using DG.Tweening;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Content.Scripts.GameCore.Scenes.Root.Views
{
    public class NavigationController : MonoBehaviour
    {
        private const float HideButtonDuration = 0.2f;
        private const float ShowButtonDuration = 0.3f;

        [Header("BUTTONS")]
        [SerializeField] private Button returnButton;
        [SerializeField] private Button logoutButton;

        [Header("OTHER")]
        [SerializeField] private RootViewController rootViewController;

        private readonly CompositeDisposable disposables = new();

        private readonly Vector3 minScale = new(0.01f, 0.01f, 0.01f);
        private readonly Vector3 defaultScale = Vector3.one;

        private Sequence sequence;
        private Tween defaultScaleTween;
        private Transform activeButtonTransform;

        private void OnDestroy()
        {
            disposables.Dispose();
        }

        public void Initialize()
        {
            rootViewController.LayoutChange.Subscribe(LayoutChangeHandle).AddTo(disposables);

            logoutButton.gameObject.SetActive(true);
            returnButton.gameObject.SetActive(false);

            logoutButton.transform.localScale = defaultScale;
            returnButton.transform.localScale = minScale;

            activeButtonTransform = logoutButton.transform;
        }

        private void LayoutChangeHandle(LayoutType layoutType)
        {
            ButtonSwitch(layoutType == LayoutType.Start ? logoutButton.transform : returnButton.transform);
        }

        private void ButtonSwitch(Transform buttonTransform)
        {
            sequence?.Kill(true);

            if (buttonTransform == activeButtonTransform)
            {
                return;
            }

            sequence = DOTween.Sequence()
                .Append(activeButtonTransform.transform.DOScale(minScale, HideButtonDuration)
                    .OnComplete(() =>
                    {
                        activeButtonTransform.gameObject.SetActive(false);
                        buttonTransform.gameObject.SetActive(true);
                    }))
                .Append(buttonTransform.DOScale(defaultScale, ShowButtonDuration))
                .Play()
                .OnComplete(() =>
                {
                    activeButtonTransform.localScale = minScale;

                    activeButtonTransform = buttonTransform;

                    activeButtonTransform.localScale = defaultScale;
                });
        }
    }
}
