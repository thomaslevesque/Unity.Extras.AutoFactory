using System;
using Microsoft.Practices.ObjectBuilder2;
using Microsoft.Practices.Unity;

namespace Unity.AutoFactory
{
    public class AutoFactory<TConcreteResult> : InjectionMember
    {
        public override void AddPolicies(Type serviceType, Type implementationType, string name, IPolicyList policies)
        {
            var type = serviceType ?? implementationType;
            policies.Set(
                typeof (IAutoFactoryPolicy),
                new AutoFactoryPolicy(type, typeof (TConcreteResult)),
                new NamedTypeBuildKey(type, name));
        }
    }
}
