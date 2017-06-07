using System;

namespace Unity.Extras.AutoFactory
{
    class AutoFactoryPolicy : IAutoFactoryPolicy
    {
        public AutoFactoryPolicy(Type factoryType, Type concreteResultType)
        {
            FactoryType = factoryType;
            ConcreteResultType = concreteResultType;
        }

        public Type FactoryType { get; }
        public Type ConcreteResultType { get; }
    }
}