using Content.Scripts.GameCore.Base.Interfaces;
using DG.Tweening;
using System.Threading.Tasks;
using UniRx;
using UnityEngine;

namespace Content.Scripts.GameCore.Base
{
    public abstract class LayoutBase : MonoBehaviour, ILayout
    {
        private const float HideLayoutDuration = 0.2f;
        private const float ShowLayoutDuration = 0.3f;
        
        private readonly Vector3 minScale = new Vector3(0.01f, 0.01f, 0.01f);
        private readonly Vector3 defaultScale = Vector3.one;
        
        private readonly CompositeDisposable disposables = new CompositeDisposable();

        private void OnDisable()
        {
            disposables.Clear();
        }

        private void OnDestroy()
        {
            disposables.Dispose();
        }

        public abstract Task SetLayoutVisible(bool value);

        public abstract void SetButtonsInteractable(bool value);

        internal abstract void Initialize();

        internal async Task HideLayout(Transform childTransform)
        {
            await childTransform.DOScale(minScale, HideLayoutDuration).SetEase(Ease.InExpo).OnComplete(() =>
            {
                OnLayoutHiding();
                SetEnabled(false);
                ChangeScale(childTransform, defaultScale);
            }).AsyncWaitForCompletion();
        }

        internal async Task ShowLayout(Transform targetTransform)
        {
            ChangeScale(targetTransform, minScale);
            SetEnabled(true);
            OnLayoutShowing();

            await targetTransform.DOScale(defaultScale, ShowLayoutDuration).SetEase(Ease.OutExpo).AsyncWaitForCompletion();
        }

        protected virtual void OnLayoutShowing() { }

        protected virtual void OnLayoutHiding() { }

        private void ChangeScale(Transform targetTransform, Vector3 value)
        {
            targetTransform.localScale = value;
        }

        private void SetEnabled(bool value)
        {
            if (gameObject.activeSelf == value)
            {
                return;
            }

            gameObject.SetActive(value);
        }
    }
}
