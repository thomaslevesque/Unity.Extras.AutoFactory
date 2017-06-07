using Microsoft.Practices.Unity;
using Microsoft.Practices.Unity.ObjectBuilder;

namespace Unity.Extras.AutoFactory
{
    public class AutoFactoryExtension : UnityContainerExtension
    {
        protected override void Initialize()
        {
            Context.Strategies.AddNew<AutoFactoryStrategy>(UnityBuildStage.PreCreation);
        }
    }
}