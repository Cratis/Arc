// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Commands;
using Cratis.Arc.DependencyInjection;

namespace Cratis.Arc.Chronicle.ReadModels;

/// <summary>Validates one member's relationship graph without reading application values.</summary>
internal static class ChainedReadModelLookup
{
    /// <summary>Validates the whole graph before returning a stable acquisition order.</summary>
    /// <param name="command">The command whose member is being bound.</param>
    /// <param name="parameters">The complete injected member parameters.</param>
    /// <param name="models">The registered read-model types.</param>
    /// <param name="mode">The effective read-protection mode.</param>
    /// <returns>The dependency-first order, preserving independent declaration order.</returns>
    internal static ParameterInfo[] Order(Type command, ParameterInfo[] parameters, IReadOnlySet<Type> models, ReadProtectionMode mode)
    {
        var declaration = new Declaration(command, models, mode);
        var dependencies = new Dictionary<ParameterInfo, ParameterInfo>();
        foreach (var target in parameters)
        {
            if (Attribute.GetCustomAttribute(target, typeof(LookUpByAttribute), inherit: true) is not LookUpByAttribute { Parameter: not null } lookup) continue;
            var source = Validate(declaration, parameters, target, lookup);
            dependencies.Add(target, source);
        }
        if (dependencies.Count == 0) return parameters;
        return Sort(command, parameters, dependencies);
    }

    /// <summary>Reads a relationship from an already acquired sibling in this frame.</summary>
    /// <param name="command">The command being bound.</param>
    /// <param name="target">The dependent parameter.</param>
    /// <param name="lookup">The validated chained declaration.</param>
    /// <returns>The relationship value, or null for an absent source.</returns>
    /// <exception cref="InvalidLookUpBy">The source has not been acquired in this binding frame.</exception>
    internal static object? ValueFor(Type command, ParameterInfo target, LookUpByAttribute lookup)
    {
        var parameters = ((MethodBase)target.Member).GetParameters();
        var source = parameters.Single(parameter => parameter.Name == lookup.Parameter);
        if (ParameterDependencyBatch.Current is not { } batch || !batch.TryGet(source, out var value))
        {
            throw Invalid(command, target, lookup, "the source must be acquired by binding the complete member");
        }
        return value is null ? null : ChainedReadModelKeys.PropertyFor(source.ParameterType, lookup.Property)!.Read(value);
    }

    static ParameterInfo Validate(Declaration declaration, ParameterInfo[] parameters, ParameterInfo target, LookUpByAttribute lookup)
    {
        var command = declaration.Command;
        if (Attribute.IsDefined(target, typeof(LookUpByAttribute<>), inherit: true))
        {
            throw Invalid(command, target, lookup, "only one lookup form may be declared");
        }
        var source = parameters.SingleOrDefault(parameter => parameter.Name == lookup.Parameter);
        if (source is null || source == target) throw Invalid(command, target, lookup, "name a different sibling parameter in the same member");
        ValidateModel(declaration, target, lookup, target);
        ValidateModel(declaration, target, lookup, source);
        ValidateProperty(command, target, lookup, source);
        return source;
    }

    static void ValidateProperty(Type command, ParameterInfo target, LookUpByAttribute lookup, ParameterInfo source)
    {
        var property = ChainedReadModelKeys.PropertyFor(source.ParameterType, lookup.Property)
            ?? throw Invalid(command, target, lookup, "the source property must be public, readable, non-indexed and registered by generated code");
        if (!CommandReadModelKeys.TryGetKeyType(target.ParameterType, out var keyType) || keyType is null)
        {
            throw Invalid(command, target, lookup, "the target key type must be registered by generated code");
        }
        if ((Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType) != keyType)
        {
            throw Invalid(command, target, lookup, $"the source property type must exactly match target key type '{keyType}'");
        }
    }

    static void ValidateModel(Declaration declaration, ParameterInfo target, LookUpByAttribute lookup, ParameterInfo parameter)
    {
        if (!IsProtectedModel(declaration, parameter))
        {
            throw Invalid(declaration.Command, target, lookup, $"parameter '{parameter.Name}' must be a registered plain protected read model with effective ReadProtectionMode.On");
        }
    }

    static bool IsProtectedModel(Declaration declaration, ParameterInfo parameter)
    {
        if (declaration.Mode != ReadProtectionMode.On) return false;
        if (!parameter.ParameterType.IsClass) return false;
        if (Attribute.IsDefined(parameter, typeof(UnprotectedAttribute)) || Attribute.IsDefined(parameter.ParameterType, typeof(UnprotectedAttribute))) return false;
        return declaration.Models.Contains(parameter.ParameterType) && !PerStreamReadModelBinding.IsDeclared(parameter.ParameterType);
    }

    static ParameterInfo[] Sort(Type command, ParameterInfo[] parameters, Dictionary<ParameterInfo, ParameterInfo> dependencies)
    {
        var ordered = new List<ParameterInfo>();
        var remaining = parameters.ToList();
        while (remaining.Count > 0)
        {
            var next = remaining.FirstOrDefault(parameter => !dependencies.TryGetValue(parameter, out var source) || ordered.Contains(source))
                ?? throw Cycle(command, remaining, dependencies);
            ordered.Add(next);
            remaining.Remove(next);
        }
        return ordered.ToArray();
    }

    static InvalidLookUpBy Cycle(Type command, List<ParameterInfo> remaining, Dictionary<ParameterInfo, ParameterInfo> dependencies)
    {
        var path = new List<ParameterInfo>();
        var cursor = remaining[0];
        while (!path.Contains(cursor))
        {
            path.Add(cursor);
            cursor = dependencies[cursor];
        }
        var cycle = path.Skip(path.IndexOf(cursor)).Append(cursor).Select(parameter => parameter.Name);
        return new(command, string.Empty, $"member '{cursor.Member.Name}' has a chained lookup cycle: {string.Join(" -> ", cycle)}");
    }

    static InvalidLookUpBy Invalid(Type command, ParameterInfo target, LookUpByAttribute lookup, string reason) =>
        new(command, lookup.Property, $"member '{target.Member.Name}', parameter '{target.Name}', source '{lookup.Parameter}': {reason}");

    sealed record Declaration(Type Command, IReadOnlySet<Type> Models, ReadProtectionMode Mode);
}
