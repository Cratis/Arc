// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using Cratis.Arc.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Validation;

/// <summary>
/// Preflights an entire fresh constructor graph without executing application code or resolving services.
/// </summary>
/// <param name="manifest">The executing root's immutable registrations.</param>
/// <param name="validatorType">The top-level validator for refusal diagnostics.</param>
internal sealed class ProtectedValidatorDependencyPlan(ProtectedDecisionRegistrationManifest manifest, Type validatorType)
{
    readonly List<ProtectedValidatorActivationPlan> _reads = [];

    /// <summary>
    /// Builds a complete validator plan, refusing opaque or conflicting registrations before activation.
    /// </summary>
    /// <returns>The inspected plan.</returns>
    /// <exception cref="DiscoverableValidatorRefusedInProtectedDecision">The validator or a dependency has no eligible fresh construction plan.</exception>
    internal ProtectedValidatorActivationPlan Build()
    {
        if (!manifest.HasTrustedReader())
        {
            throw Refuse();
        }
        var bindings = manifest.BindingsFor(validatorType);
        Action<object>? configure = null;
        if (bindings.Length == 0)
        {
            if (!manifest.Discovered(validatorType))
            {
                throw Refuse();
            }
        }
        else if (bindings is [var descriptor] && !descriptor.IsKeyedService &&
            descriptor.Lifetime == ServiceLifetime.Transient && manifest.OriginOf(descriptor) is { } origin &&
            (origin.IsConvention || origin.Configure is not null))
        {
            configure = origin.Configure;
        }
        else
        {
            throw Refuse();
        }
        return ConstructorPlan(validatorType, [], configure);
    }

    /// <summary>
    /// Resolves and verifies every read leaf after full preflight and before any graph constructor runs.
    /// </summary>
    /// <param name="provider">The attested executing scope.</param>
    internal void ResolveReads(IServiceProvider provider)
    {
        // A valid reader is required even for a configured validator without direct read dependencies.
        var readerType = manifest.ReaderType!;
        var reader = provider.GetService(readerType);
        manifest.OriginOf(manifest.BindingsFor(readerType)[^1])!.VerifyRead!(reader);
        foreach (var read in _reads)
        {
            var value = read.Type == readerType ? reader : provider.GetService(read.Type);
            read.VerifyRead!(value);
            read.Value = value;
        }
    }

    ProtectedValidatorActivationPlan Dependency(Type type, HashSet<Type> path)
    {
        var bindings = manifest.BindingsFor(type);
        if (type == manifest.ReaderType)
        {
            return Read(type, manifest.OriginOf(bindings[^1])!.VerifyRead!);
        }
        if (manifest.ReadVerifierFor(type) is { } verify)
        {
            return Read(type, verify);
        }
        if (Unsupported(type) || bindings is not [var descriptor] || descriptor.IsKeyedService ||
            descriptor.Lifetime != ServiceLifetime.Transient || descriptor.ImplementationType is not { } implementation ||
            implementation.IsAbstract || implementation.ContainsGenericParameters || Unsupported(implementation))
        {
            throw Refuse();
        }
        return ConstructorPlan(implementation, path, null);
    }

    ProtectedValidatorActivationPlan ConstructorPlan(Type type, HashSet<Type> path, Action<object>? configure)
    {
        if (type.IsAbstract || type.ContainsGenericParameters || !path.Add(type) ||
            DiscoverableValidators.PublicConstructorsOf(type) is not [var constructor])
        {
            throw Refuse();
        }
        var parameters = constructor.GetParameters();
        var arguments = new List<ProtectedValidatorActivationPlan>();
        foreach (var parameter in parameters)
        {
            if (parameter.IsOptional || parameter.HasDefaultValue || parameter.GetCustomAttributesData().Any(attribute => attribute.AttributeType.Assembly != typeof(Attribute).Assembly))
            {
                // Only runtime metadata (including nullable annotations) is inert here. Do not ignore
                // application binding attributes such as keyed services, lookup keys or advisory reads.
                throw Refuse();
            }
            arguments.Add(Dependency(parameter.ParameterType, path));
        }
        path.Remove(type);
        return new(type, constructor, arguments.ToArray(), configure, null);
    }

    ProtectedValidatorActivationPlan Read(Type type, Action<object?> verify)
    {
        var plan = new ProtectedValidatorActivationPlan(type, null, [], null, verify);
        _reads.Add(plan);
        return plan;
    }

    bool Unsupported(Type type) => type.IsValueType || type.IsArray || type.ContainsGenericParameters ||
        typeof(Delegate).IsAssignableFrom(type) || typeof(IEnumerable).IsAssignableFrom(type) ||
        typeof(IServiceProvider).IsAssignableFrom(type) || typeof(IServiceScopeFactory).IsAssignableFrom(type) ||
        typeof(IServiceScope).IsAssignableFrom(type) || type == typeof(string) || manifest.Forbids(type);

    DiscoverableValidatorRefusedInProtectedDecision Refuse() => DiscoverableValidatorRefusedInProtectedDecision.NotConstructedByArc(validatorType);
}
