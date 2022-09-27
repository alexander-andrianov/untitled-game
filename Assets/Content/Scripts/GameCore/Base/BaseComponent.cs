using Interfaces;
using System;
using System.Threading.Tasks;

namespace Content
{
    public abstract class BaseComponent : IComponent, IInitialize, IDisposable
    {
        public event Action InitializeStarting;
        public event Action InitializeFinished;
        public event Action InitializeFaild;

        public ComponentState State { get; protected set; }

        public async virtual Task Initialize()
        {
            State = ComponentState.Starting;
            InitializeStarting?.Invoke();

            try
            {
                await Initialize();

                State = ComponentState.Finished;
                InitializeFinished?.Invoke();
            }
            catch (Exception)
            {
                State = ComponentState.Failed;
                InitializeFaild?.Invoke();
            }
        }

        protected virtual async Task InitializeComponent()
        {
            await Task.CompletedTask;
        }

        public virtual void Dispose() { }
    }
}
