// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.Queries.ModelBound;

/// <summary>
/// Determines whether a reflected method can be exposed as a model-bound query.
/// </summary>
internal static class ModelBoundQueryMethod
{
    /// <summary>
    /// Checks whether a method is an ordinary public or internal static query candidate.
    /// </summary>
    /// <param name="method">The method to check.</param>
    /// <returns>True when the method is eligible for query return-type validation.</returns>
    /// <remarks>Attribute names are compared rather than runtime types because proxy generation uses MetadataLoadContext.</remarks>
    internal static bool IsCandidate(MethodInfo method) =>
        (method.IsPublic || method.IsAssembly) &&
        method.IsStatic &&
        !method.ContainsGenericParameters &&
        !method.IsSpecialName &&
        !method.Name.Contains('<') &&
        !method.GetCustomAttributesData().Any(_ => _.AttributeType.Name == "CompilerGeneratedAttribute") &&
        !IsExcluded(method);

    static bool IsExcluded(MethodInfo method) =>
        method.GetCustomAttributesData().Any(_ => _.AttributeType.FullName == "Cratis.Arc.Queries.ModelBound.NotAQueryAttribute") ||
        IsFoldCreator(method);

    static bool IsFoldCreator(MethodInfo method)
    {
        if (!method.IsStatic || method.Name != "Create" || !ReturnsDeclaringType(method))
        {
            return false;
        }

        var parameters = method.GetParameters();
        if (parameters.Length is < 1 or > 2)
        {
            return false;
        }

        var parameterTypes = parameters.Select(_ => ResolveMetadataType(() => UnwrapNullable(_.ParameterType))).ToArray();

        return parameterTypes.Any(_ => _?.FullName == "Cratis.Chronicle.Events.EventContext") ||
            (parameterTypes[0] is { } firstParameter && IsEvent(firstParameter));
    }

    static bool ReturnsDeclaringType(MethodInfo method)
    {
        var type = method.ReturnType;
        if (type.IsGenericType &&
            (type.GetGenericTypeDefinition().FullName == "System.Threading.Tasks.Task`1" ||
                type.GetGenericTypeDefinition().FullName == "System.Threading.Tasks.ValueTask`1"))
        {
            type = type.GetGenericArguments()[0];
        }

        return type == method.DeclaringType;
    }

    static Type UnwrapNullable(Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition().FullName == "System.Nullable`1"
            ? type.GetGenericArguments()[0]
            : type;

    static bool IsEvent(Type type)
    {
        for (var current = (Type?)type; current is not null; current = ResolveMetadataType(() => current.BaseType))
        {
            if (current.GetCustomAttributesData().Any(_ => ResolveMetadataType(() => _.AttributeType)?.FullName == "Cratis.Chronicle.Events.EventTypeAttribute"))
            {
                return true;
            }
        }

        return false;
    }

    static Type? ResolveMetadataType(Func<Type?> resolve)
    {
        try
        {
            return resolve();
        }
        catch (Exception error) when (error is FileNotFoundException or TypeLoadException)
        {
            // Missing metadata cannot prove a fold creator. Keep the method eligible for the existing query checks.
            return null;
        }
    }
}
