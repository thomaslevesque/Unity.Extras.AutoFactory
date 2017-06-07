using Microsoft.Practices.ObjectBuilder2;

namespace Unity.Extras.AutoFactory
{
    class AutoFactoryStrategy : BuildPlanStrategy
    {
        public override void PreBuildUp(IBuilderContext context)
        {
            if (context.Existing == null)
            {
                var policy = context.Policies.Get<IAutoFactoryPolicy>(context.OriginalBuildKey);
                if (policy != null)
                {
                    var autoFactoryType = AutoFactoryTypeGenerator.GetAutoFactoryType(context.OriginalBuildKey.Type, policy.ConcreteResultType);
                    context.Existing = context.NewBuildUp(new NamedTypeBuildKey(autoFactoryType, context.OriginalBuildKey.Name));
                }
            }
            base.PreBuildUp(context);
        }
    }
}