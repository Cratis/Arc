// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Cratis.Arc.Commands;
using Cratis.Arc.DependencyInjection;
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
        var invalidValidators = candidates.Where(_ => !DerivesFromAbstractValidatorOf(_, DiscoverableModelTypeOf(_))).ToArray();

        if (invalidValidators.Length > 0)
        {
            throw new DiscoverableValidatorMustImplementAbstractValidator(invalidValidators[0]);
        }

        // Key by the model the validator declares through IDiscoverableValidator<T>; the check above has
        // already proven it derives from AbstractValidator<T> for that same model, whatever its intermediate bases.
        _validatorTypesByModelType = candidates.ToDictionary(DiscoverableModelTypeOf, _ => _);
    }

    /// <inheritdoc/>
    public bool TryGet(Type modelType, [MaybeNullWhen(false)] out IValidator validator) =>
        TryGet(modelType, _serviceProviderAccessor(), out validator);

    /// <inheritdoc/>
    public bool TryGet(Type modelType, IServiceProvider serviceProvider, [MaybeNullWhen(false)] out IValidator validator)
    {
        if (_validatorTypesByModelType.TryGetValue(modelType, out var value))
        {
            if (CommandDecisionPolicy.IsProtected)
            {
                throw new InvalidOperationException($"Discoverable validator '{value}' cannot run in a protected decision command; see Arc#2831.");
            }

            validator = (Construct(serviceProvider, value) as IValidator)!;
            return true;
        }

        validator = null;
        return false;
    }

    /// <summary>
    /// Gets the model type a validator declares through <see cref="IDiscoverableValidator{T}"/>.
    /// </summary>
    /// <param name="type">The candidate validator type.</param>
    /// <returns>The model type.</returns>
    static Type DiscoverableModelTypeOf(Type type) =>
        type.GetInterfaces()
            .Single(_ => _.IsGenericType && _.GetGenericTypeDefinition() == typeof(IDiscoverableValidator<>))
            .GetGenericArguments()[0];

    /// <summary>
    /// Determines whether a type derives from <see cref="AbstractValidator{T}"/> closed over the given model type.
    /// </summary>
    /// <param name="type">The candidate validator type.</param>
    /// <param name="modelType">The model type the validator must be for.</param>
    /// <returns>True when the type derives from <c>AbstractValidator&lt;modelType&gt;</c>; otherwise false.</returns>
    /// <remarks>
    /// This replaces <c>IsAssignableTo(typeof(AbstractValidator&lt;&gt;).MakeGenericType(modelType))</c>. Constructing a
    /// closed generic through <c>MakeGenericType</c> at runtime is not statically analyzable and breaks under
    /// NativeAOT/trimming, whereas reading the generic argument off an already-constructed base type in the chain is
    /// AOT-safe and preserves the exact "must be an AbstractValidator for this model" semantics — including rejecting a
    /// validator whose <see cref="AbstractValidator{T}"/> is closed over a different model type.
    /// </remarks>
    static bool DerivesFromAbstractValidatorOf(Type type, Type modelType)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (current.IsGenericType &&
                current.GetGenericTypeDefinition() == typeof(AbstractValidator<>) &&
                current.GetGenericArguments()[0] == modelType)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Constructs a validator from the supplied provider.
    /// </summary>
    /// <remarks>Legacy commands follow command parameter binding semantics.</remarks>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> to resolve dependencies from.</param>
    /// <param name="validatorType">The validator type to construct.</param>
    /// <returns>The constructed validator instance.</returns>
    static object Construct(IServiceProvider serviceProvider, Type validatorType)
    {
        var isService = serviceProvider.GetService(typeof(IServiceProviderIsService)) as IServiceProviderIsService;

        // Legacy commands retain registered validators of every lifetime and their existing construction rules.
        var safetyChecks = serviceProvider.GetService(typeof(IEnumerable<ICommandDependencySafety>)) as IEnumerable<ICommandDependencySafety> ?? [];
        if (isService?.IsService(validatorType) != false)
        {
            foreach (var safety in safetyChecks) safety.ValidateRegisteredValidator(validatorType);
        }

        var registered = serviceProvider.GetService(validatorType);
        if (registered is not null) return registered;

        var legacyConstructor = validatorType.GetConstructors()
            .OrderByDescending(_ => _.GetParameters().Length)
            .First();
        var legacyArguments = ParameterDependencyResolver.Resolve(
            serviceProvider,
            legacyConstructor.GetParameters(),
            parameter => new CannotResolveValidatorDependency(validatorType, parameter));
        return legacyConstructor.Invoke(legacyArguments);
    }
}
