// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Projections.ModelBound;

namespace Cratis.Arc.Chronicle.ReadModels;

/// <summary>
/// Identifies model-bound read models that Chronicle discovers as standalone projections.
/// </summary>
/// <remarks>
/// Mirrors the root-selection traversal in Chronicle's internal ModelBoundProjections discovery. The client artifacts
/// provider exposes candidates, not roots; keep this traversal aligned when upgrading Chronicle until it exposes a
/// shared discovery API. Explicit fluent projection and reducer targets are registered separately.
/// </remarks>
static class ModelBoundReadModelRoots
{
    /// <summary>
    /// Gets the candidate types that are not used as children or subobjects by another candidate.
    /// </summary>
    /// <param name="candidates">All model-bound projection candidates.</param>
    /// <returns>The standalone model-bound read model types.</returns>
    public static IEnumerable<Type> Discover(IEnumerable<Type> candidates)
    {
        var types = candidates.ToArray();
        var referencedTypes = new HashSet<Type>();
        foreach (var type in types)
        {
            var referencedByType = new HashSet<Type>();
            CollectReferences(type, referencedByType);
            referencedByType.Remove(type);
            referencedTypes.UnionWith(referencedByType);
        }

        return types.Except(referencedTypes);
    }

    static void CollectReferences(Type type, HashSet<Type> referencedTypes)
    {
        var constructor = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .OrderByDescending(_ => _.GetParameters().Length)
            .FirstOrDefault();
        var parameters = constructor?.GetParameters() ?? [];
        foreach (var parameter in parameters)
        {
            CollectMemberReferences(parameter.ParameterType, parameter.GetCustomAttributes(), referencedTypes);
        }

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (parameters.Any(_ => string.Equals(_.Name, property.Name, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            CollectMemberReferences(property.PropertyType, property.GetCustomAttributes(), referencedTypes);
        }
    }

    static void CollectMemberReferences(Type type, IEnumerable<Attribute> attributes, HashSet<Type> referencedTypes)
    {
        var hasChildren = attributes.Any(_ => _.GetType().IsGenericType &&
            _.GetType().GetGenericTypeDefinition() == typeof(ChildrenFromAttribute<>));
        var referencedType = hasChildren ? GetChildType(type) : IsComplexType(type) ? type : null;
        if (referencedType is not null && referencedTypes.Add(referencedType))
        {
            CollectReferences(referencedType, referencedTypes);
        }
    }

    static Type? GetChildType(Type type)
    {
        if (type.IsGenericType)
        {
            var definition = type.GetGenericTypeDefinition();
            if (definition == typeof(IEnumerable<>) || definition.GetInterfaces().Any(IsEnumerable))
            {
                return type.GetGenericArguments()[0];
            }
        }

        return type.GetInterfaces().FirstOrDefault(IsEnumerable)?.GetGenericArguments()[0];
    }

    static bool IsEnumerable(Type type) => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>);

    static bool IsComplexType(Type type)
    {
        if (type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal) ||
            type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(TimeSpan) || type == typeof(Guid))
        {
            return false;
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            return false;
        }

        return type.IsClass || type.IsValueType;
    }
}
