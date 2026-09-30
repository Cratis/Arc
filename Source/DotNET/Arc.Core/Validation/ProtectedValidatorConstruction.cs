// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using Cratis.Arc.Commands;
using Cratis.Types;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Validation;

/// <summary>
/// Constructs and certifies the discoverable validators a protected decision command may run.
/// </summary>
/// <remarks>
/// What is certified is narrow: the validator's only public constructor takes no parameters, so nothing is injected
/// through it, and Arc ran that constructor itself, so no registration factory supplied the instance. The certificate
/// is kept with the constructed instance, not with a service descriptor or a provider, so copying descriptors, cloning
/// or wrapping a provider, or changing the service collection after the provider was built cannot forge it. It also
/// records, by reference, the instance's rule list and the components of each rule, so an instance that gained, lost
/// or swapped a rule or a rule component is not certified.
/// <para>
/// It does not cover anything else. State of the instance changed after construction is not seen: a decorator that
/// returns the inner instance, property injection, code that resolves the instance and sets a field or property a rule
/// closure reads, a condition applied to an existing rule, a changed cascade mode, or rules added to a nested child
/// validator. Neither are static members or service locators inside the validator. Keeping those out of the validators
/// of protected decisions is the developer's responsibility.
/// </para>
/// </remarks>
internal static class ProtectedValidatorConstruction
{
    const int InitialSnapshotCapacity = 16;

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
        var validator = ConstructUncertified(validatorType);
        _constructed.AddOrUpdate(validator, SnapshotOf(validator));
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
        _constructed.TryGetValue(validator, out var snapshot) &&
        MatchesSnapshot(validator, snapshot);

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
    /// <para>
    /// Only a transient validator resolved outside a protected decision skips the certificate: nothing shares that
    /// instance with a later protected decision, so the rule snapshot would be wasted work on every other pipeline. A
    /// scoped or singleton validator is always certified, because a later protected decision may reuse the instance.
    /// </para>
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
                var lifetime = descriptor.Lifetime;
                services[index] = new ServiceDescriptor(
                    implementationType,
                    _ => lifetime == ServiceLifetime.Transient && !CommandDecisionPolicy.RefusesDiscoverableValidators
                        ? ConstructUncertified(implementationType)
                        : Construct(implementationType),
                    lifetime);
            }
        }
    }

    static object ConstructUncertified(Type validatorType) =>
        ParameterlessConstructorOf(validatorType)!.Invoke(BindingFlags.DoNotWrapExceptions, null, null, null);

    static ConstructorInfo? ParameterlessConstructorOf(Type validatorType) =>
        _parameterlessConstructors.GetOrAdd(validatorType, static type =>
            !type.IsAbstract && !type.ContainsGenericParameters && DiscoverableValidators.PublicConstructorsOf(type) is [{ } constructor] && constructor.GetParameters().Length == 0
                ? constructor
                : null);

    /// <summary>
    /// Captures the rules of a validator and the components of each rule, in order, so rules added or extended after construction are detected.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <returns>The rules and their components, followed by a null terminator.</returns>
    /// <remarks>
    /// This runs for every certified construction, so it avoids LINQ and enumerator allocations where it can. The array
    /// always ends in at least one null, which marks where the snapshot stops.
    /// </remarks>
    static object[] SnapshotOf(object validator)
    {
        var snapshot = new object[InitialSnapshotCapacity];
        var count = 0;
        if (validator is IEnumerable<IValidationRule> rules)
        {
            foreach (var rule in rules)
            {
                Append(ref snapshot, ref count, rule);
                if (rule.Components is IList components)
                {
                    for (var index = 0; index < components.Count; index++)
                    {
                        Append(ref snapshot, ref count, components[index]!);
                    }
                }
                else
                {
                    foreach (var component in rule.Components)
                    {
                        Append(ref snapshot, ref count, component);
                    }
                }
            }
        }

        return snapshot;
    }

    static void Append(ref object[] snapshot, ref int count, object item)
    {
        if (count == snapshot.Length - 1)
        {
            Array.Resize(ref snapshot, snapshot.Length * 2);
        }

        snapshot[count++] = item;
    }

    /// <summary>
    /// Determines whether a validator still has exactly the rules and rule components captured in a snapshot.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="snapshot">The snapshot captured at construction.</param>
    /// <returns>True when nothing was added, removed or replaced; otherwise false.</returns>
    static bool MatchesSnapshot(object validator, object[] snapshot)
    {
        var position = 0;
        if (validator is IEnumerable<IValidationRule> rules)
        {
            foreach (var rule in rules)
            {
                if (!Matches(snapshot, ref position, rule))
                {
                    return false;
                }

                if (rule.Components is IList components)
                {
                    for (var index = 0; index < components.Count; index++)
                    {
                        if (!Matches(snapshot, ref position, components[index]!))
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    foreach (var component in rule.Components)
                    {
                        if (!Matches(snapshot, ref position, component))
                        {
                            return false;
                        }
                    }
                }
            }
        }

        return snapshot[position] is null;
    }

    static bool Matches(object[] snapshot, ref int position, object item) =>
        position < snapshot.Length && ReferenceEquals(snapshot[position++], item);
}
