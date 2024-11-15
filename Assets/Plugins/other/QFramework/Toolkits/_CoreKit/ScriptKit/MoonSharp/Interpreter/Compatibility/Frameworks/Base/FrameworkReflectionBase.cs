﻿#if DOTNET_CORE
	using TTypeInfo = System.Reflection.TypeInfo;
#elif NETFX_CORE
	using TTypeInfo = System.Reflection.TypeInfo;
#else
using TTypeInfo = System.Type;
#endif
using System;
using System.Linq;
using System.Reflection;

namespace MoonSharp.Interpreter.Compatibility.Frameworks
{
    internal abstract class FrameworkReflectionBase : FrameworkBase
    {
        public abstract TTypeInfo GetTypeInfoFromType(TTypeInfo t);

        public override Assembly GetAssembly(TTypeInfo t)
        {
            return GetTypeInfoFromType(t).Assembly;
        }

        public override TTypeInfo GetBaseType(TTypeInfo t)
        {
            return GetTypeInfoFromType(t).BaseType;
        }


        public override bool IsValueType(TTypeInfo t)
        {
            return GetTypeInfoFromType(t).IsValueType;
        }

        public override bool IsInterface(TTypeInfo t)
        {
            return GetTypeInfoFromType(t).IsInterface;
        }

        public override bool IsNestedPublic(TTypeInfo t)
        {
            return GetTypeInfoFromType(t).IsNestedPublic;
        }

        public override bool IsAbstract(TTypeInfo t)
        {
            return GetTypeInfoFromType(t).IsAbstract;
        }

        public override bool IsEnum(TTypeInfo t)
        {
            return GetTypeInfoFromType(t).IsEnum;
        }

        public override bool IsGenericTypeDefinition(TTypeInfo t)
        {
            return GetTypeInfoFromType(t).IsGenericTypeDefinition;
        }

        public override bool IsGenericType(TTypeInfo t)
        {
            return GetTypeInfoFromType(t).IsGenericType;
        }

        public override Attribute[] GetCustomAttributes(Type t, bool inherit)
        {
            return GetTypeInfoFromType(t).GetCustomAttributes(inherit).OfType<Attribute>().ToArray();
        }

        public override Attribute[] GetCustomAttributes(Type t, Type at, bool inherit)
        {
            return GetTypeInfoFromType(t).GetCustomAttributes(at, inherit).OfType<Attribute>().ToArray();
        }
    }
}