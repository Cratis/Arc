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
    readonly Func<IServiceProvider> _serviceProviderAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="DiscoverableValidators"/> class.
    /// </summary>
    /// <param name="types"><see cref="ITypes"/> for type discovery.</param>
    public DiscoverableValidators(ITypes types) : this(types, () => Internals.ServiceProvider)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DiscoverableValidators"/> class.
    /// </summary>
    /// <param name="types"><see cref="ITypes"/> for type discovery.</param>
    /// <param name="serviceProviderAccessor">Callback for getting the <see cref="IServiceProvider"/> to resolve validators from.</param>
    internal DiscoverableValidators(ITypes types, Func<IServiceProvider> serviceProviderAccessor)
    {
        _serviceProviderAccessor = serviceProviderAccessor;
        var candidates = types.FindMultiple(typeof(IDiscoverableValidator<>));
        var invalidValidators = candidates.Where(_ =>
        {
            var interfaces = _.GetInterfaces();
            var validatorType = interfaces.Single(_ => _.IsGenericType && _.GetGenericTypeDefinition() == typeof(IDiscoverableValidator<>));
            var modelType = validatorType.GetGenericArguments()[0];
            return !_.IsAssignableTo(typeof(AbstractValidator<>).MakeGenericType(modelType));
        }).ToArray();

        if (invalidValidators.Length > 0)
        {
            throw new DiscoverableValidatorMustImplementAbstractValidator(invalidValidators[0]);
        }

        _validatorTypesByModelType = candidates
            .ToDictionary(
                _ =>
                {
                    var current = _.BaseType!;
                    while (!current.IsDerivedFromOpenGeneric(typeof(AbstractValidator<>)))
                    {
                        current = current.BaseType!;
                    }
                    return current.GetGenericArguments()[0];
                },
                _ => _);
    }

    /// <summary>
    /// Gets the discovered validator types.
    /// </summary>
    internal IEnumerable<Type> ValidatorTypes => _validatorTypesByModelType.Values;

    /// <inheritdoc/>
    public bool TryGet(Type modelType, [MaybeNullWhen(false)] out IValidator validator)
    {
        if (_validatorTypesByModelType.TryGetValue(modelType, out var value))
        {
            validator = (_serviceProviderAccessor().GetRequiredService(value) as IValidator)!;
            return true;
        }

        validator = null;
        return false;
    }
}
