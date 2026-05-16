// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Cratis.Reflection;
using Cratis.Types;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Validation;

/// <summary>
/// Represents an implementation of <see cref="IDiscoverableValidators"/> that can discover validators from assemblies.
/// </summary>
public class DiscoverableValidators : IDiscoverableValidators
{
    readonly Dictionary<Type, Type> _validatorTypesByModelType;

    /// <summary>
    /// Initializes a new instance of the <see cref="DiscoverableValidators"/> class.
    /// </summary>
    /// <param name="types"><see cref="ITypes"/> for type discovery.</param>
    public DiscoverableValidators(ITypes types)
    {
        var candidates = types.FindMultiple(typeof(IDiscoverableValidator<>));
        var invalidValidators = candidates.Where(IsInvalidDiscoverableValidator).ToArray();

        if (invalidValidators.Length > 0)
        {
            throw new DiscoverableValidatorMustImplementAbstractValidator(invalidValidators[0]);
        }

        _validatorTypesByModelType = candidates
            .ToDictionary(GetModelTypeFromValidator, _ => _);
    }

    /// <inheritdoc/>
    public bool TryGet(Type modelType, [MaybeNullWhen(false)] out IValidator validator)
    {
        if (_validatorTypesByModelType.TryGetValue(modelType, out var value))
        {
            validator = (Internals.ServiceProvider.GetRequiredService(value) as IValidator)!;
            return true;
        }

        validator = null;
        return false;
    }

    [UnconditionalSuppressMessage("AOT", "IL2070", Justification = "Candidate types are discovered via ITypes.FindMultiple which preserves interfaces. Source-generated type discovery is the long-term fix (tracked in GitHub issue #2204).")]
    static bool IsInvalidDiscoverableValidator(Type candidateType)
    {
        var interfaces = candidateType.GetInterfaces();
        var validatorType = interfaces.Single(_ => _.IsGenericType && _.GetGenericTypeDefinition() == typeof(IDiscoverableValidator<>));
        var modelType = validatorType.GetGenericArguments()[0];
        return !interfaces.Any(i =>
            i.IsGenericType &&
            i.GetGenericTypeDefinition() == typeof(IValidator<>) &&
            i.GetGenericArguments()[0] == modelType);
    }

    static Type GetModelTypeFromValidator(Type candidateType)
    {
        var current = candidateType.BaseType!;
        while (!current.IsDerivedFromOpenGeneric(typeof(AbstractValidator<>)))
        {
            current = current.BaseType!;
        }
        return current.GetGenericArguments()[0];
    }
}
