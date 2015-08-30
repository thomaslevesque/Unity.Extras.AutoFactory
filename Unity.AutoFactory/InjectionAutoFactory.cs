using System;
using Microsoft.Practices.ObjectBuilder2;
using Microsoft.Practices.Unity;

namespace Unity.AutoFactory
{
    public class InjectionAutoFactory<TConcreteResult> : InjectionMember
    {
        public override void AddPolicies(Type serviceType, Type implementationType, string name, IPolicyList policies)
        {
            policies.Set(
                typeof (IAutoFactoryPolicy),
                new AutoFactoryPolicy(implementationType, typeof (TConcreteResult)),
                new NamedTypeBuildKey(implementationType, name));
        }
    }
}
