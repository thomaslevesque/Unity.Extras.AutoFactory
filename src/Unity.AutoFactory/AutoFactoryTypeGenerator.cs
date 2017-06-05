using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Microsoft.Practices.Unity;

namespace Unity.AutoFactory
{
    static class AutoFactoryTypeGenerator
    {
        private static readonly ConcurrentDictionary<Tuple<Type, Type>, Type> AutoFactoryTypeCache;
        private static readonly ModuleBuilder Module;

        static AutoFactoryTypeGenerator()
        {
            AutoFactoryTypeCache = new ConcurrentDictionary<Tuple<Type, Type>, Type>();
            var assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(
                new AssemblyName("AutoFactories"),
                AssemblyBuilderAccess.Run);
            Module = assembly.DefineDynamicModule("AutoFactories");
        }

        public static Type GetAutoFactoryType(Type factoryType, Type concreteResultType)
        {
            return AutoFactoryTypeCache.GetOrAdd(
                Tuple.Create(factoryType, concreteResultType),
                t => CreateAutoFactoryType(t.Item1, t.Item2));
        }

        private static Type CreateAutoFactoryType(Type factoryType, Type concreteResultType)
        {
            if (!factoryType.IsInterface)
                throw new InvalidOperationException("Factory type must be an interface");
            if (concreteResultType.IsInterface || concreteResultType.IsAbstract || concreteResultType.IsEnum || concreteResultType.IsSubclassOf(typeof(Delegate)))
                throw new InvalidOperationException("Concrete result type must be a concrete class or struct");

            var methods = factoryType.GetMethods();

            var badMethod = methods.FirstOrDefault(m => !m.ReturnType.IsAssignableFrom(concreteResultType));
            if (badMethod != null)
                throw new InvalidOperationException($"Method '{badMethod}' has the wrong return type");

            string typeName = $"{factoryType.FullName.Replace('.', '_')}_{concreteResultType.FullName.Replace('.', '_')}_AutoFactory";
            var autoFactoryType = Module.DefineType(typeName);
            autoFactoryType.AddInterfaceImplementation(factoryType);
            var containerField = autoFactoryType.DefineField("_container", typeof(IUnityContainer), FieldAttributes.Private | FieldAttributes.InitOnly);
            CreateConstructor(autoFactoryType, containerField);
            foreach (var method in methods)
            {
                CreateMethod(autoFactoryType, containerField, method, concreteResultType);
            }

            var builtType = autoFactoryType.CreateType();
            return builtType;
        }

        private static void CreateMethod(TypeBuilder type, FieldBuilder containerField, MethodInfo interfaceMethod, Type concreteResultType)
        {
            var parameters = interfaceMethod.GetParameters();
            var paramTypes = Array.ConvertAll(parameters, p => p.ParameterType);
            var method = type.DefineMethod(
                interfaceMethod.Name,
                (interfaceMethod.Attributes | MethodAttributes.Final) & ~MethodAttributes.Abstract,
                interfaceMethod.ReturnType,
                paramTypes);

            foreach (var param in parameters)
            {
                method.DefineParameter(param.Position + 1, param.Attributes, param.Name);
            }

            var ctor = GetBestMatchConstructor(concreteResultType, interfaceMethod);
            if (ctor == null)
                throw new InvalidOperationException($"Type '{concreteResultType}' doesn't have a constructor compatible with method '{interfaceMethod}'");

            var il = method.GetILGenerator();

            // ParameterOverrides overrides;
            il.DeclareLocal(typeof(ParameterOverrides));

            // overrides = new ParameterOverrides
            // ReSharper disable once AssignNullToNotNullAttribute
            il.Emit(OpCodes.Newobj, typeof(ParameterOverrides).GetConstructor(Type.EmptyTypes));
            il.Emit(OpCodes.Stloc_0);


            var addMethod = typeof(ParameterOverrides).GetMethod("Add");
            var methodParams = interfaceMethod.GetParameters().ToDictionary(p => p.Name);
            foreach (var param in ctor.GetParameters())
            {
                ParameterInfo createParam;
                if (!methodParams.TryGetValue(param.Name, out createParam))
                    continue;

                // overrides.Add(paramName, paramValue)
                il.Emit(OpCodes.Ldloc_0);
                il.Emit(OpCodes.Ldstr, param.Name);
                il.Emit(OpCodes.Ldarg_S, createParam.Position + 1);
                if (createParam.ParameterType.IsValueType)
                    il.Emit(OpCodes.Box, createParam.ParameterType);
                il.Emit(OpCodes.Callvirt, addMethod);
            }

            il.Emit(OpCodes.Ldarg_0); // this
            il.Emit(OpCodes.Ldfld, containerField); // ._container

            // typeof(TConcreteResult)
            il.Emit(OpCodes.Ldtoken, concreteResultType);
            il.Emit(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle"));

            il.Emit(OpCodes.Ldnull); // null (name)

            // new ResolverOverride[1] { overrides }
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Newarr, typeof(ResolverOverride));
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Stelem_Ref);

            // container.Resolve(typeof(TConcreteResult), null, overrides);
            il.EmitCall(OpCodes.Callvirt, typeof(IUnityContainer).GetMethod("Resolve"), new[] { typeof(ParameterOverrides) });

            il.Emit(OpCodes.Ret);
        }

        private static ConstructorInfo GetBestMatchConstructor(Type typeToConstruct, MethodInfo createMethod)
        {
            var createParams = createMethod.GetParameters();
            var ctors = typeToConstruct.GetConstructors();
            var eligibleCtors = ctors.Where(c => IsEligibleConstructor(c, createParams));
            return eligibleCtors.OrderByDescending(c => c.GetParameters().Length).FirstOrDefault();
        }

        private static bool IsEligibleConstructor(ConstructorInfo ctor, ParameterInfo[] createParams)
        {
            var ctorParams = ctor.GetParameters();
            var joinedByName =
                (from createParam in createParams
                 join ctorParam in ctorParams
                 on createParam.Name equals ctorParam.Name
                 select new { createParam, ctorParam }).ToList();

            if (joinedByName.Count < createParams.Length)
                return false;

            foreach (var x in joinedByName)
            {
                if (!x.ctorParam.ParameterType.IsAssignableFrom(x.createParam.ParameterType))
                    return false;
            }

            return true;
        }

        private static void CreateConstructor(TypeBuilder type, FieldBuilder field)
        {
            var ctor = type.DefineConstructor(
                MethodAttributes.Public,
                CallingConventions.Standard,
                new[] { typeof(IUnityContainer) });
            ctor.DefineParameter(1, ParameterAttributes.None, "container");
            var il = ctor.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            // ReSharper disable once AssignNullToNotNullAttribute (I know this ctor exists...)
            il.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes));
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Stfld, field);
            il.Emit(OpCodes.Ret);
        }
    }
}
