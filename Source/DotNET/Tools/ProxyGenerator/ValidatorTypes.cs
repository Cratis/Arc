// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.CompilerServices;

namespace Cratis.Arc.ProxyGenerator;

/// <summary>
/// Finds executable validators in the generated assembly and its managed application dependencies.
/// </summary>
internal static class ValidatorTypes
{
    static readonly HashSet<string> _validatorBaseTypes =
    [
        "FluentValidation.AbstractValidator`1",
        "Cratis.Arc.Validation.BaseValidator`1",
        "Cratis.Arc.Validation.DiscoverableValidator`1",
        "Cratis.Arc.Commands.CommandValidator`1",
        "Cratis.Arc.Queries.QueryValidator`1"
    ];

    static readonly HashSet<string> _frameworkAssemblyNames = ["System", "Microsoft", "netstandard", "mscorlib"];
    static readonly ConditionalWeakTable<Assembly, Dictionary<Type, List<Type>>> _validatorsByAssembly = new();

    /// <summary>
    /// Finds the validator for a type, rejecting duplicate discoverable validators for the requested type.
    /// </summary>
    /// <param name="generatedAssembly">The assembly being generated.</param>
    /// <param name="type">The type being validated.</param>
    /// <returns>The validator, or <see langword="null"/> if none is found.</returns>
    /// <exception cref="MultipleValidatorsForType">More than one discoverable validator is found for the type.</exception>
    internal static Type? Find(Assembly generatedAssembly, Type type)
    {
        var assemblies = TypeExtensions.ValidatorAssemblies
            .Select(_ => RuntimeValidatorAssemblies.For(_) ?? _)
            .Append(generatedAssembly)
            .Append(type.Assembly)
            .Where(_ => !IsFrameworkAssembly(_))
            .Distinct();

        return Select(type, assemblies.SelectMany(assembly =>
            _validatorsByAssembly.GetValue(assembly, Index).GetValueOrDefault(type) ?? []));
    }

    /// <summary>
    /// Finds a validator using a supplied type loader, for testing assembly load failures.
    /// </summary>
    /// <param name="generatedAssembly">The assembly to inspect.</param>
    /// <param name="type">The type being validated.</param>
    /// <param name="loadTypes">The loader for types from the assembly.</param>
    /// <returns>The matching validator, or null if none exists.</returns>
    internal static Type? Find(Assembly generatedAssembly, Type type, Func<Assembly, Type[]> loadTypes) =>
        Select(type, Index(generatedAssembly, loadTypes).GetValueOrDefault(type) ?? []);

    static Type? Select(Type type, IEnumerable<Type> candidates)
    {
        var distinct = candidates.Distinct().OrderBy(_ => _.FullName, StringComparer.Ordinal).ToArray();
        var discoverable = distinct.Where(IsDiscoverable).ToArray();
        if (discoverable.Length > 1)
        {
            throw new MultipleValidatorsForType(type, discoverable[0], discoverable[1]);
        }

        return discoverable.FirstOrDefault() ?? distinct.FirstOrDefault();
    }

    static bool IsDiscoverable(Type candidate) => candidate.GetInterfaces().Any(_ =>
        _.IsGenericType && _.GetGenericTypeDefinition().FullName == "Cratis.Arc.Validation.IDiscoverableValidator`1");

    static bool IsFrameworkAssembly(Assembly assembly)
    {
        var name = assembly.GetName().Name;
        return name is not null && (_frameworkAssemblyNames.Contains(name) ||
            name.StartsWith("System.", StringComparison.Ordinal) ||
            name.StartsWith("Microsoft.", StringComparison.Ordinal));
    }

    static Dictionary<Type, List<Type>> Index(Assembly assembly) => Index(assembly, _ => _.GetTypes());

    static Dictionary<Type, List<Type>> Index(Assembly assembly, Func<Assembly, Type[]> loadTypes)
    {
        Type[] types;
        try
        {
            types = loadTypes(assembly);
        }
        catch (ReflectionTypeLoadException exception)
        {
            throw new ValidatorTypesCouldNotBeLoaded(assembly, exception);
        }

        var validators = new Dictionary<Type, List<Type>>();
        foreach (var candidate in types.OrderBy(_ => _.FullName, StringComparer.Ordinal))
        {
            if (candidate.IsAbstract || candidate.IsInterface)
            {
                continue;
            }

            for (var baseType = candidate.BaseType; baseType is not null; baseType = baseType.BaseType)
            {
                if (baseType.IsGenericType && _validatorBaseTypes.Contains(baseType.GetGenericTypeDefinition().FullName!))
                {
                    var validatedType = baseType.GetGenericArguments()[0];
                    if (!validators.TryGetValue(validatedType, out var matches))
                    {
                        matches = [];
                        validators.Add(validatedType, matches);
                    }

                    if (!matches.Contains(candidate))
                    {
                        matches.Add(candidate);
                    }
                }
            }
        }

        return validators;
    }
}
