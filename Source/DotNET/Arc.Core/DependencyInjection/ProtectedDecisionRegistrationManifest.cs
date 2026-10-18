// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.DependencyInjection;

/// <summary>
/// Holds private, frozen registration evidence for one built root.
/// </summary>
/// <param name="registrations">The final ordered descriptors.</param>
/// <param name="origins">The operations on the original collection.</param>
/// <param name="validators">The discovered validator universe.</param>
/// <param name="forbidden">Raw services and read models that cannot be collaborators.</param>
/// <param name="readerType">Chronicle's reader contract.</param>
internal sealed class ProtectedDecisionRegistrationManifest(
    IReadOnlyList<ServiceDescriptor> registrations,
    Dictionary<ServiceDescriptor, ProtectedDecisionRegistrationOrigin> origins,
    HashSet<Type> validators,
    HashSet<Type> forbidden,
    Type? readerType)
{
    /// <summary>
    /// Gets the reader contract supplied by Chronicle, if present.
    /// </summary>
    internal Type? ReaderType { get; } = readerType;

    /// <summary>
    /// Gets every matching binding, including keyed and open generic registrations.
    /// </summary>
    /// <param name="type">The dependency contract.</param>
    /// <returns>The ordered matching descriptors.</returns>
    internal ServiceDescriptor[] BindingsFor(Type type) => registrations.Where(descriptor =>
        descriptor.ServiceType == type || (type.IsGenericType && descriptor.ServiceType == type.GetGenericTypeDefinition())).ToArray();

    /// <summary>
    /// Gets the operation that inserted this exact descriptor on the original collection.
    /// </summary>
    /// <param name="descriptor">The descriptor.</param>
    /// <returns>The origin, or null for application or copied bindings.</returns>
    internal ProtectedDecisionRegistrationOrigin? OriginOf(ServiceDescriptor descriptor) => origins.GetValueOrDefault(descriptor);

    /// <summary>
    /// Gets a closed framework read verifier, allowing only an unselected Arc convention generic fallback.
    /// </summary>
    /// <param name="type">The exact read type requested.</param>
    /// <returns>The verifier, or null when any matching registration lacks eligible origin evidence.</returns>
    internal Action<object?>? ReadVerifierFor(Type type)
    {
        var bindings = BindingsFor(type);
        var exact = bindings.Where(binding => binding.ServiceType == type).ToArray();
        if (exact is not [var selected] || selected.IsKeyedService || selected.Lifetime != ServiceLifetime.Transient ||
            OriginOf(selected)?.VerifyRead is not { } verify)
        {
            return null;
        }
        return bindings.All(binding => ReferenceEquals(binding, selected) ||
            (!binding.IsKeyedService && binding.ServiceType.IsGenericTypeDefinition &&
                binding.ImplementationType == binding.ServiceType && binding.Lifetime == ServiceLifetime.Transient &&
                OriginOf(binding) is { IsConvention: true })) ? verify : null;
    }

    /// <summary>
    /// Determines whether the validator was discovered before the root was built.
    /// </summary>
    /// <param name="type">The validator type.</param>
    /// <returns>Whether it belongs to this root's universe.</returns>
    internal bool Discovered(Type type) => validators.Contains(type);

    /// <summary>
    /// Determines whether Chronicle forbids a collaborator type.
    /// </summary>
    /// <param name="type">The collaborator type.</param>
    /// <returns>Whether it is a raw service or plain read model.</returns>
    internal bool Forbids(Type type) => forbidden.Any(contract => contract.IsAssignableFrom(type) ||
        (type.IsGenericType && type.GetGenericTypeDefinition() == contract));

    /// <summary>
    /// Checks all reader origins, not merely the effective last registration.
    /// </summary>
    /// <returns>Whether only framework readers precede the effective scoped command-aware reader.</returns>
    internal bool HasTrustedReader()
    {
        if (ReaderType is null)
        {
            return false;
        }
        var bindings = BindingsFor(ReaderType);
        return bindings.Length > 0 && bindings.All(descriptor =>
            !descriptor.IsKeyedService && OriginOf(descriptor) is { } origin && (origin.IsCommandReader || origin.IsSupersededReader || origin.IsChronicleClientReader)) &&
            bindings[^1].Lifetime == ServiceLifetime.Scoped && OriginOf(bindings[^1])!.IsCommandReader;
    }
}
