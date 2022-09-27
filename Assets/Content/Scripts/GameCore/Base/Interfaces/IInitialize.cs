using System;
using System.Threading.Tasks;

namespace Interfaces
{
    public interface IInitialize
    {
        event Action InitializeStarting;
        event Action InitializeFinished;
        event Action InitializeFaild;

        ComponentState State { get; }

        Task Initialize();
    }
}
