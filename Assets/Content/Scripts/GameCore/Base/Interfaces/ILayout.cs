using System.Threading.Tasks;

namespace Content.Scripts.GameCore.Base.Interfaces
{
    internal interface ILayout
    {
        void SetButtonsInteractable(bool value);
        Task SetLayoutVisible(bool value);
    }
}