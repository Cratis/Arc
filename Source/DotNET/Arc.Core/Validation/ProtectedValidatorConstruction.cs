// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using Cratis.Types;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Validation;

/// <summary>
/// Constructs and certifies the discoverable validators a protected decision command may run.
/// </summary>
/// <remarks>
/// A protected command only runs a validator whose rules can depend on nothing but the model it validates: its only
/// public constructor takes no parameters, so nothing is injected into it, and Arc constructed the instance itself, so
/// no registration factory added rules or captured state. The evidence lives with the constructed instance, not with a
/// service descriptor or a provider, so copying descriptors, cloning or wrapping a provider, or changing the service
/// collection after the provider was built cannot forge it. An instance Arc did not construct, or whose rules changed
/// after construction, is not certified.
/// </remarks>
internal static class ProtectedValidatorConstruction
{
    static readonly ConditionalWeakTable<object, object[]> _constructed = new();
    static readonly ConcurrentDictionary<Type, ConstructorInfo?> _parameterlessConstructors = new();

    /// <summary>
    /// Determines whether a validator type takes no dependencies: it is concrete and its only public constructor is parameterless.
    /// </summary>
    /// <param name="validatorType">The validator type.</param>
    /// <returns>True when the validator takes no dependencies; otherwise false.</returns>
    internal static bool IsDependencyFree(Type validatorType) => ParameterlessConstructorOf(validatorType) is not null;

    /// <summary>
    /// Constructs a dependency-free validator and records it as constructed by Arc.
    /// </summary>
    /// <param name="validatorType">The validator type, which <see cref="IsDependencyFree"/> has accepted.</param>
    /// <returns>The constructed validator.</returns>
    internal static object Construct(Type validatorType)
    {
        var validator = ParameterlessConstructorOf(validatorType)!.Invoke(BindingFlags.DoNotWrapExceptions, null, null, null);
        _constructed.AddOrUpdate(validator, RulesOf(validator));
        return validator;
    }

    /// <summary>
    /// Determines whether a validator instance is exactly of the given type, was constructed by Arc, and still has the
    /// rules it was constructed with.
    /// </summary>
    /// <param name="validator">The validator instance.</param>
    /// <param name="validatorType">The expected validator type.</param>
    /// <returns>True when the instance is certified; otherwise false.</returns>
    internal static bool IsCertified(object validator, Type validatorType) =>
        validator.GetType() == validatorType &&
        _constructed.TryGetValue(validator, out var rules) &&
        rules.SequenceEqual(RulesOf(validator), ReferenceEqualityComparer.Instance);

    /// <summary>
    /// Makes the container construct every convention-bound, dependency-free discoverable validator through Arc, so a
    /// validator the container resolves can be certified.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> holding the bindings.</param>
    /// <param name="types">The <see cref="ITypes"/> to discover validators from.</param>
    /// <remarks>
    /// Only a plain type binding of a validator to itself is replaced; it constructs the same instance through the
    /// same parameterless constructor with the same lifetime. A factory, instance or other implementation registration
    /// is left untouched and is therefore not certified.
    /// </remarks>
    internal static void ConstructDependencyFreeValidatorsThroughArc(this IServiceCollection services, ITypes types)
    {
        var validatorTypes = types.FindMultiple(typeof(IDiscoverableValidator<>)).Where(IsDependencyFree).ToHashSet();
        for (var index = 0; index < services.Count; index++)
        {
            var descriptor = services[index];
            if (!descriptor.IsKeyedService &&
                descriptor.ImplementationType is { } implementationType &&
                implementationType == descriptor.ServiceType &&
                validatorTypes.Contains(implementationType))
            {
                services[index] = new ServiceDescriptor(implementationType, _ => Construct(implementationType), descriptor.Lifetime);
            }
        }
    }

    static ConstructorInfo? ParameterlessConstructorOf(Type validatorType) =>
        _parameterlessConstructors.GetOrAdd(validatorType, static type =>
            !type.IsAbstract && !type.ContainsGenericParameters && DiscoverableValidators.PublicConstructorsOf(type) is [{ } constructor] && constructor.GetParameters().Length == 0
                ? constructor
                : null);

    /// <summary>
    /// Captures the rules of a validator and the components of each rule, so rules added or extended after construction are detected.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <returns>The rules and their components, in order.</returns>
    static object[] RulesOf(object validator) =>
        validator is IEnumerable<IValidationRule> rules
            ? [.. rules.SelectMany(rule => rule.Components.Cast<object>().Prepend(rule))]
            : [];
}
