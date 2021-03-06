﻿using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace SerializableActions.Internal
{
    /// <summary>
    /// Serializable wrapper for a MethodInfo.
    /// </summary>
    [Serializable]
    public class SerializableMethod
    {
        [SerializeField]
        private string methodName;

        [SerializeField]
        private SerializableSystemType containingType;
        [SerializeField]
        private BindingFlags bindingFlags;
        [SerializeField]
        private bool isGeneric;
        [SerializeField]
        private SerializeableParameterType[] parameterTypes;
        [SerializeField]
        private string[] parameterNames;

        /// <summary>
        /// Name of the method
        /// </summary>
        public string MethodName { get { return methodName; } }

        /// <summary>
        /// The parameter types of the method.
        /// If the wrapped method is Foo(int i, string s), this array is [int, string]
        /// </summary>
        public SerializeableParameterType[] ParameterTypes { get { return parameterTypes; } }

        /// <summary>
        /// The parameter names of the method.
        /// If the wrapped method is Foo(int i, string s), this array is [i, s]
        /// </summary>
        public string[] ParameterNames { get { return parameterNames; } }

        public SerializableMethod(MethodInfo method)
        {
            methodInfo = method;

            methodName = method.Name;
            containingType = method.DeclaringType;
            isGeneric = method.IsGenericMethod;

            ExtractParameters(method, isGeneric, out parameterTypes, out parameterNames);

            bindingFlags =
                (method.IsPublic ? BindingFlags.Public : BindingFlags.NonPublic) |
                (method.IsStatic ? BindingFlags.Static : BindingFlags.Instance);

        }

        private void ExtractParameters(MethodInfo method, bool isGeneric, out SerializeableParameterType[] paramTypes, out string[] paramNames)
        {
            var rawParameters = method.GetParameters();
            paramTypes = new SerializeableParameterType[rawParameters.Length];
            paramNames = new string[rawParameters.Length];

            for (int i = 0; i < rawParameters.Length; i++)
            {
                paramTypes[i] = new SerializeableParameterType(rawParameters[i].ParameterType);
                paramNames[i] = rawParameters[i].Name;
            }

            if (isGeneric)
            {
                var genericVersion = method.GetGenericMethodDefinition();
                var genericParams = genericVersion.GetParameters();
                for (int i = 0; i < genericParams.Length; i++)
                {
                    paramTypes[i].IsGeneric = genericParams[i].ParameterType.IsGenericParameter;
                }
            }
        }

        private MethodInfo methodInfo;
        public MethodInfo MethodInfo
        {
            get
            {
                if (methodInfo == null && !string.IsNullOrEmpty(methodName))
                {
                    Type[] types = new Type[parameterTypes.Length];
                    for (int i = 0; i < types.Length; i++)
                    {
                        types[i] = parameterTypes[i].SystemType;
                    }

                    var allMethods = containingType.SystemType.GetMethods(bindingFlags).Where(method => method.Name == methodName);
                    foreach (var method in allMethods)
                    {
                        if (ParametersMatch(method.GetParameters(), parameterTypes))
                        {
                            methodInfo = method;
                            break;
                        }
                    }

                    if (methodInfo != null && isGeneric)
                    {
                        Type[] generics = (from param in parameterTypes where param.IsGeneric select param.SystemType).ToArray();
                        methodInfo = methodInfo.MakeGenericMethod(generics);
                    }

                    if (methodInfo == null)
                    {
                        return null;
                    }
                }
                return methodInfo;
            }
        }

        private bool ParametersMatch(ParameterInfo[] parameters, SerializeableParameterType[] serializedParameters)
        {
            if (parameters.Length != serializedParameters.Length)
                return false;

            for (int i = 0; i < parameters.Length; i++)
            {
                //Special-case for generics, just check that both are marked as generic
                if (parameters[i].ParameterType.IsGenericParameter)
                {
                    if (!serializedParameters[i].IsGeneric)
                    {
                        return false;
                    }
                }
                else
                {
                    if (parameters[i].ParameterType != serializedParameters[i].SystemType)
                        return false;
                }
            }

            return true;
        }

        private bool Equals(SerializableMethod other)
        {
            return string.Equals(methodName, other.methodName) &&
                   Equals(containingType, other.containingType) &&
                   bindingFlags == other.bindingFlags &&
                   isGeneric == other.isGeneric &&
                   Util.ArraysEqual(parameterTypes, other.parameterTypes);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != GetType())
                return false;
            return Equals((SerializableMethod) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (methodName != null ? methodName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (containingType != null ? containingType.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (int) bindingFlags;
                hashCode = (hashCode * 397) ^ isGeneric.GetHashCode();
                hashCode = (hashCode * 397) ^ (parameterTypes != null ? parameterTypes.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}