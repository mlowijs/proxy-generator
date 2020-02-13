using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace ProxyGenerator
{
    public class CilProxyGenerator : IProxyGenerator
    {
        private static readonly MethodInfo GetTypeFromHandleMethod =
            typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle));

        private readonly AssemblyBuilder _assemblyBuilder;
        private readonly ModuleBuilder _moduleBuilder;

        private readonly Dictionary<(Type serviceType, Type proxyType), Type> _typeCache =
            new Dictionary<(Type, Type), Type>();

        public CilProxyGenerator()
        {
            _assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("CilServiceProxyAssembly"),
                AssemblyBuilderAccess.Run);
            
            _moduleBuilder = _assemblyBuilder.DefineDynamicModule("CilServiceProxyModule");
        }
        
        public Type GenerateProxy<TService, TProxy>()
            where TProxy : InterceptingDecorator<TService>
        {
            var serviceType = typeof(TService);
            var proxyType = typeof(TProxy);

            var cacheKey = (serviceType, proxyType);

            if (_typeCache.ContainsKey(cacheKey))
                return _typeCache[cacheKey];

            var typeBuilder = GetTypeBuilder(serviceType, proxyType);
            
            var methods = serviceType.GetMethods(BindingFlags.Public | BindingFlags.Instance);

            for (var i = 0; i < methods.Length; i++)
            {
                var methodBuilder = DefineMethod(typeBuilder, methods[i]);
                GenerateMethod<TService>(methodBuilder, methods[i], i);
            }

            GenerateConstructor(typeBuilder, proxyType);

            var type = typeBuilder.CreateType();
            _typeCache[cacheKey] = type;

            return type;
        }

        public TService CreateProxy<TService, TProxy>(params object[] args)
            where TProxy : InterceptingDecorator<TService>
        {
            var type = GenerateProxy<TService, TProxy>();

            return (TService)Activator.CreateInstance(type, args);
        }

        private TypeBuilder GetTypeBuilder(Type serviceType, Type proxyType)
        {
            return _moduleBuilder.DefineType($"{serviceType.Name}_proxy",
                TypeAttributes.Public | TypeAttributes.Class, proxyType, new[] {serviceType});
        }

        private MethodBuilder DefineMethod(TypeBuilder typeBuilder, MethodInfo methodInfo)
        {
            var parameters = methodInfo.GetParameters();
            var genericParameters = methodInfo.GetGenericArguments();

            var methodBuilder = typeBuilder.DefineMethod(methodInfo.Name,
                MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig,
                methodInfo.CallingConvention, methodInfo.ReturnType, parameters.Select(p => p.ParameterType).ToArray());

            // Add generic arguments and constraints to methodInfo if generic method
            if (!methodInfo.IsGenericMethod)
                return methodBuilder;
            
            var genericTypeParameterBuilders =
                methodBuilder.DefineGenericParameters(genericParameters.Select(ga => ga.Name).ToArray());

            for (var i = 0; i < genericParameters.Length; i++)
            {
                var genericParameter = genericParameters[i];
                var genericParameterBuilder = genericTypeParameterBuilders[i];
                
                genericParameterBuilder.SetGenericParameterAttributes(genericParameter.GenericParameterAttributes);
                
                var constraints = genericParameter.GetGenericParameterConstraints();
            
                genericParameterBuilder.SetBaseTypeConstraint(constraints.SingleOrDefault(c => c.IsClass));
                genericParameterBuilder.SetInterfaceConstraints(constraints.Where(c => c.IsInterface).ToArray());
            }

            return methodBuilder;
        }
        
        /*
         * Following method generates this code, more or less:
         *
         * var args = new object[] { arg0, arg1, ... };
         * var typeArgs = new Type[] { typeof(T0), typeof(T1), ... };
         * return this.Invoke(methodIndex, args, typeArgs);
         */
        private void GenerateMethod<TService>(MethodBuilder methodBuilder, MethodInfo methodInfo, int methodIndex)
        {
            var parameters = methodInfo.GetParameters();

            var ilGenerator = methodBuilder.GetILGenerator();

            ilGenerator.Emit(OpCodes.Ldarg_0); // Load "this" for Invoke call
            ilGenerator.Emit(OpCodes.Ldc_I4, methodIndex); // Load "methodIndex" for Invoke call

            // Create args array: var args = new object[parameters.Length];
            ilGenerator.Emit(OpCodes.Ldc_I4, parameters.Length); // Load parameter count
            ilGenerator.Emit(OpCodes.Newarr, typeof(object)); // Create new object[] on the stack

            // Put the args into the object array: args[0] = arg0; args[1] = arg1; ...etc
            for (var i = 0; i < parameters.Length; i++)
            {
                ilGenerator.Emit(OpCodes.Dup); // Duplicate args reference
                ilGenerator.Emit(OpCodes.Ldc_I4, i); // Load array index
                ilGenerator.Emit(OpCodes.Ldarg, i + 1); // Load argument

                // If argument is a value type, box it: (object)arg
                var parameterType = parameters[i].ParameterType;
                
                if (parameterType.IsValueType) 
                    ilGenerator.Emit(OpCodes.Box, parameterType);
                
                ilGenerator.Emit(OpCodes.Stelem_Ref); // Store argument into the array: args[i] = arg;
            }
            
            if (methodInfo.IsGenericMethod)
            {
                var genericParameters = methodInfo.GetGenericArguments();
                
                // Create type args array: var typeArgs = new Type[genericParameters.Length];
                ilGenerator.Emit(OpCodes.Ldc_I4, genericParameters.Length); // Load parameter count
                ilGenerator.Emit(OpCodes.Newarr, typeof(Type)); // Create new Type[] on the stack

                // Put the type args into the array: typeArgs[0] = typeof(TArg0); typeArgs[1] = typeof(TArg1); ...etc
                for (var i = 0; i < genericParameters.Length; i++)
                {
                    ilGenerator.Emit(OpCodes.Dup); // Duplicate typeArgs reference
                    ilGenerator.Emit(OpCodes.Ldc_I4, i); // Load array index
                    ilGenerator.Emit(OpCodes.Ldtoken, genericParameters[i]); // Load type handle
                    ilGenerator.Emit(OpCodes.Call, GetTypeFromHandleMethod); // Get type from handle and load
                    ilGenerator.Emit(OpCodes.Stelem_Ref); // Store argument into the array: typeArgs[i] = typeof(T);
                }
            }
            else
            {
                ilGenerator.Emit(OpCodes.Ldnull); // Load null onto the stack
            }

            // Call: this.Invoke(methodIndex, args, typeArgs);
            ilGenerator.Emit(OpCodes.Callvirt, InterceptingDecorator<TService>.InvokeMethod);

            // Return the return value if the method does not return void
            if (methodInfo.ReturnType == typeof(void))
                ilGenerator.Emit(OpCodes.Pop);
            else
                ilGenerator.Emit(OpCodes.Unbox_Any, methodInfo.ReturnType);
            
            ilGenerator.Emit(OpCodes.Ret);
        }

        private void GenerateConstructor(TypeBuilder typeBuilder, Type proxyType)
        {
            var baseConstructor = proxyType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)[0];

            var ctorParameters = baseConstructor.GetParameters().Select(p => p.ParameterType).ToArray();
            
            var ctorBuilder = typeBuilder.DefineConstructor(baseConstructor.Attributes | MethodAttributes.Public,
                baseConstructor.CallingConvention, ctorParameters);

            var ctorGenerator = ctorBuilder.GetILGenerator();
            
            // Load "this" and all constructor arguments
            for (var i = 0; i <= ctorParameters.Length; i++)
                ctorGenerator.Emit(OpCodes.Ldarg, i);
            
            ctorGenerator.Emit(OpCodes.Call, baseConstructor); // Call base constructor
            ctorGenerator.Emit(OpCodes.Ret); // Return
        }
    }
}