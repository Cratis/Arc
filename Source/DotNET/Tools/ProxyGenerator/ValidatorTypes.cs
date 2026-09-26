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
    static readonly ConditionalWeakTable<Assembly, ValidatorIndex> _validatorsByAssembly = new();
    static readonly HashSet<Assembly> _warnedAssemblies = [];
    static Action<string> _warning = _ => { };

    /// <summary>
    /// Finds the validator for a type, rejecting duplicate discoverable validators for the requested type.
    /// </summary>
    /// <param name="generatedAssembly">The assembly being generated.</param>
    /// <param name="type">The type being validated.</param>
    /// <returns>The validator, or <see langword="null"/> if none is found.</returns>
    /// <exception cref="MultipleValidatorsForType">More than one discoverable validator is found for the type.</exception>
    internal static Type? Find(Assembly generatedAssembly, Type type) => Find(
        generatedAssembly,
        type,
        TypeExtensions.ValidatorAssemblies,
        assembly => _validatorsByAssembly.GetValue(assembly, Index),
        _warning,
        _warnedAssemblies);

    /// <summary>
    /// Configures the generation run's existing message logger for partial dependency loads.
    /// </summary>
    /// <param name="warning">The generation message callback.</param>
    internal static void SetWarningLogger(Action<string> warning)
    {
        _warning = warning;
        _warnedAssemblies.Clear();
    }

    /// <summary>
    /// Finds a validator using a supplied type loader, for testing assembly load failures.
    /// </summary>
    /// <param name="generatedAssembly">The assembly to inspect.</param>
    /// <param name="type">The type being validated.</param>
    /// <param name="loadTypes">The loader for types from the assembly.</param>
    /// <returns>The matching validator, or null if none exists.</returns>
    internal static Type? Find(Assembly generatedAssembly, Type type, Func<Assembly, Type[]> loadTypes) =>
        SelectStrict(type, generatedAssembly, Index(generatedAssembly, loadTypes));

    /// <summary>
    /// Finds validators with supplied assemblies and type loader, for testing partial dependency loads.
    /// </summary>
    /// <param name="generatedAssembly">The assembly being generated.</param>
    /// <param name="type">The requested model type.</param>
    /// <param name="dependencies">Other application dependencies.</param>
    /// <param name="loadTypes">The test type loader.</param>
    /// <param name="warning">The generation message callback.</param>
    /// <returns>The matching validator or null.</returns>
    internal static Type? Find(Assembly generatedAssembly, Type type, IEnumerable<Assembly> dependencies, Func<Assembly, Type[]> loadTypes, Action<string> warning) =>
        Find(generatedAssembly, type, dependencies, assembly => Index(assembly, loadTypes), warning, []);

    static Type? Find(Assembly generatedAssembly, Type type, IEnumerable<Assembly> dependencies, Func<Assembly, ValidatorIndex> index, Action<string> warning, HashSet<Assembly> warnedAssemblies)
    {
        var generated = RuntimeValidatorAssemblies.For(generatedAssembly) ?? generatedAssembly;
        var model = RuntimeValidatorAssemblies.For(type.Assembly) ?? type.Assembly;
        var assemblies = dependencies
            .Select(_ => RuntimeValidatorAssemblies.For(_) ?? _)
            .Append(generated)
            .Append(model)
            .Where(_ => !IsFrameworkAssembly(_))
            .Distinct();

        var candidates = new List<Type>();
        foreach (var assembly in assemblies)
        {
            var validators = index(assembly);
            if (validators.LoadFailure is { } failure)
            {
                if (assembly == generated || assembly == model)
                {
                    throw new ValidatorTypesCouldNotBeLoaded(assembly, failure);
                }

                if (warnedAssemblies.Add(assembly))
                {
                    warning($"warning: Could not load all validator types from dependency assembly '{assembly.GetName().Name}'; using successfully loaded types: {string.Join("; ", failure.LoaderExceptions.Where(_ => _ is not null).Select(_ => _.Message))}");
                }
            }

            candidates.AddRange(validators.Validators.GetValueOrDefault(type) ?? []);
        }

        return Select(type, candidates);
    }

    static Type? SelectStrict(Type type, Assembly assembly, ValidatorIndex index)
    {
        if (index.LoadFailure is { } failure)
        {
            throw new ValidatorTypesCouldNotBeLoaded(assembly, failure);
        }

        return Select(type, index.Validators.GetValueOrDefault(type) ?? []);
    }

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

    static ValidatorIndex Index(Assembly assembly) => Index(assembly, _ => _.GetTypes());

    static ValidatorIndex Index(Assembly assembly, Func<Assembly, Type[]> loadTypes)
    {
        Type[] types;
        ReflectionTypeLoadException? loadFailure = null;
        try
        {
            types = loadTypes(assembly);
        }
        catch (ReflectionTypeLoadException exception)
        {
            loadFailure = exception;
            types = [.. exception.Types.OfType<Type>()];
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

        return new(validators, loadFailure);
    }

    sealed record ValidatorIndex(Dictionary<Type, List<Type>> Validators, ReflectionTypeLoadException? LoadFailure);
}
