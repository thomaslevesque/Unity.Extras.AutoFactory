using System;
using Microsoft.Practices.ObjectBuilder2;

namespace Unity.AutoFactory
{
    interface IAutoFactoryPolicy : IBuilderPolicy
    {
        Type ConcreteResultType { get; }
        Type FactoryType { get; }
    }
}