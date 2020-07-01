﻿using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace ErrorProne.NET.Core
{
    public class RoslynLayoutSizeRetriever
    {
        private readonly ConcurrentDictionary<(Type type, string propertyName), PropertyAccessor?> PropertyAccessors = new ConcurrentDictionary<(Type type, string propertyName), PropertyAccessor?>();

        public int? TryGetUnderlyingSymbolLayoutSize(object instance)
        {
            // This is a hack, but unfortunately, when the type is referenced via reference assembly,
            // type.GetAttributes() returns an empty array, so we have to look into internal API to get the structs size!
            var type = instance?.GetType();
            if (type?.Name == "NonErrorNamedTypeSymbol")
            {
                var underlyingSymbolAccessor = GetOrCreatePropertyAccessor(instance?.GetType(), "UnderlyingSymbol");
                var underlyingSymbol = underlyingSymbolAccessor?.GetValue(instance);

                var layoutAccessor = GetOrCreatePropertyAccessor(underlyingSymbol?.GetType(), "Layout");
                var layout = layoutAccessor?.GetValue(underlyingSymbol);

                var sizeAccessor = GetOrCreatePropertyAccessor(layout?.GetType(), "Size");
                var size = sizeAccessor?.GetValue(layout);

                if (size is int)
                {
                    return (int)size;
                }
            }

            return null;
        }

        private PropertyAccessor? GetOrCreatePropertyAccessor(Type? type, string propertyName)
        {
            if (type == null)
            {
                return null;
            }

            return PropertyAccessors.GetOrAdd((type, propertyName), tuple =>
            {
                return PropertyAccessor.TryCreate(tuple.type, tuple.propertyName);
            });
        }

        internal class PropertyAccessor
        {
            private readonly Delegate? _propertyGetterDelegate;
            private readonly MethodInfo _propertyGetterMethodInfo;

            public PropertyAccessor(Delegate? propertyGetterDelegate, MethodInfo propertyGetterMethodInfo)
            {
                _propertyGetterDelegate = propertyGetterDelegate;
                _propertyGetterMethodInfo = propertyGetterMethodInfo;
            }

            public static PropertyAccessor? TryCreate(Type? type, string propertyName)
            {
                if (type == null)
                {
                    return null;
                }

                var property = type.GetProperty(propertyName,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (property == null)
                {
                    return null;
                }

                var method = property.GetMethod;
                var funcType = GetFuncType(type, method);

                Delegate? propertyGetterDelegate = null;
                if (!type.IsValueType)
                {
                    // Can't create a delegate for structs.
                    propertyGetterDelegate = Delegate.CreateDelegate(funcType, null, method);
                }

                return new PropertyAccessor(propertyGetterDelegate, method);
            }

            public static Type GetFuncType(Type type, MethodInfo methodInfo)
            {
                Func<Type[], Type> getType;
                var isAction = methodInfo.ReturnType.Equals((typeof(void)));
                var types = methodInfo.GetParameters().Select(p => p.ParameterType);

                if (isAction)
                {
                    getType = Expression.GetActionType;
                }
                else
                {
                    getType = Expression.GetFuncType;
                    types = types.Concat(new[] { type, methodInfo.ReturnType });
                }

                return getType(types.ToArray());
            }

            public object? GetValue(object? instance)
            {
                if (instance == null)
                {
                    return null;
                }

                if (_propertyGetterDelegate != null)
                {
                    return _propertyGetterDelegate.DynamicInvoke(instance);
                }

                return _propertyGetterMethodInfo.Invoke(instance, Array.Empty<object>());
            }
        }
    }
}