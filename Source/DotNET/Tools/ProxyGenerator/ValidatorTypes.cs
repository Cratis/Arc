// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.CompilerServices;

namespace Cratis.Arc.ProxyGenerator;

/// <summary>
/// Finds executable validators in the generated assembly or the assembly declaring a concept.
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
    static readonly ConditionalWeakTable<Assembly, Dictionary<Type, Type>> _validatorsByAssembly = new();

    /// <summary>
    /// Finds the first validator for a type, preferring the generated assembly.
    /// </summary>
    /// <param name="generatedAssembly">The assembly being generated.</param>
    /// <param name="type">The type being validated.</param>
    /// <returns>The validator, or <see langword="null"/> if none is found.</returns>
    internal static Type? Find(Assembly generatedAssembly, Type type)
    {
        // A concept's validator normally lives beside the concept, not beside the command or query using it.
        // The first lookup preserves the existing precedence for validators in the generated assembly. If the
        // assemblies coincide, the second lookup is not made and the validator cannot be contributed twice.
        if (_validatorsByAssembly.GetValue(generatedAssembly, Index).TryGetValue(type, out var validator))
        {
            return validator;
        }

        return type.Assembly != generatedAssembly &&
            !IsFrameworkAssembly(type.Assembly) &&
            _validatorsByAssembly.GetValue(type.Assembly, Index).TryGetValue(type, out validator)
            ? validator
            : null;
    }

    static bool IsFrameworkAssembly(Assembly assembly)
    {
        var name = assembly.GetName().Name;
        return name is not null && (_frameworkAssemblyNames.Contains(name) ||
            name.StartsWith("System.", StringComparison.Ordinal) ||
            name.StartsWith("Microsoft.", StringComparison.Ordinal));
    }

    static Dictionary<Type, Type> Index(Assembly assembly)
    {
        Type[] types;
        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            // An unloadable unrelated type must not hide validators that were loaded successfully.
            types = [.. exception.Types.OfType<Type>()];
        }

        var validators = new Dictionary<Type, Type>();
        foreach (var candidate in types.OrderBy(_ => _.FullName, StringComparer.Ordinal))
        {
            try
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
                        validators.TryAdd(validatedType, candidate);
                    }
                }
            }
            catch (TypeLoadException)
            {
                // An unresolved base type does not invalidate other validators in the same assembly.
            }
            catch (FileNotFoundException)
            {
                // A referenced assembly may be missing even if this candidate type was loaded.
            }
        }

        return validators;
    }
}
