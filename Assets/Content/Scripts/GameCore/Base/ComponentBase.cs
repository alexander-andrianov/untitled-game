using Cysharp.Threading.Tasks;
using Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Content
{
    public abstract class ComponentBase<T> : BaseComponent where T : IComponent
    {
        public IEnumerable<T> Components { get; protected set; }

        private List<T> listComponents = new List<T>();

        public void AppendComponent(T component)
        {
            listComponents.Add(component);

            Components = listComponents;
        }

        protected async override Task InitializeComponent()
        {
            List<IInitialize> initializeComponents = new List<IInitialize>();

            foreach (var component in Components)
            {
                IInitialize initializeComponent = component as IInitialize;

                if (initializeComponent != null)
                {
                    initializeComponent?.Initialize();

                    initializeComponents.Add(initializeComponent);
                }
            }

            await UniTask.WaitUntil(() => initializeComponents.All(c => c.State == ComponentState.Finished));
        }

        public K GetComponent<K>() where K : T
        {
            return (K)Components.FirstOrDefault(r => r is K);
        }
    }
}
