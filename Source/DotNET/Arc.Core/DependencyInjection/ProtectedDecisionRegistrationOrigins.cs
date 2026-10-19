// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.DependencyInjection;

/// <summary>
/// Records registration operations on the original collection, never on copied descriptors.
/// </summary>
internal sealed class ProtectedDecisionRegistrationOrigins
{
    static readonly ConditionalWeakTable<IServiceCollection, ProtectedDecisionRegistrationOrigins> _collections = new();
    readonly Dictionary<ServiceDescriptor, ProtectedDecisionRegistrationOrigin> _origins = new(ReferenceEqualityComparer.Instance);
    readonly HashSet<Type> _validators = [];
    readonly HashSet<Type> _forbidden = [];
    Type? _readerType;

    /// <summary>
    /// Gets the origins owned by this exact collection.
    /// </summary>
    /// <param name="services">The original registrations.</param>
    /// <returns>The collection-local origins.</returns>
    internal static ProtectedDecisionRegistrationOrigins For(IServiceCollection services) => _collections.GetValue(services, _ => new());

    /// <summary>
    /// Records a descriptor inserted by Arc.
    /// </summary>
    /// <param name="descriptor">The exact inserted descriptor.</param>
    /// <param name="origin">The operation Arc performed.</param>
    internal void Record(ServiceDescriptor descriptor, ProtectedDecisionRegistrationOrigin origin) => _origins[descriptor] = origin;

    /// <summary>
    /// Determines whether a descriptor is a convention insertion on this collection.
    /// </summary>
    /// <param name="descriptor">The descriptor to inspect.</param>
    /// <returns>Whether its origin is a convention insertion.</returns>
    internal bool IsConvention(ServiceDescriptor descriptor) => _origins.TryGetValue(descriptor, out var origin) && origin.IsConvention;

    /// <summary>
    /// Transfers a genuine convention origin when the old parameterless factory replaces its descriptor.
    /// </summary>
    /// <param name="previous">The previous descriptor.</param>
    /// <param name="replacement">The descriptor actually installed.</param>
    internal void TransferConvention(ServiceDescriptor previous, ServiceDescriptor replacement)
    {
        if (IsConvention(previous))
        {
            _origins[replacement] = _origins[previous];
        }
    }

    /// <summary>
    /// Records the discovered validator universe at Arc registration time.
    /// </summary>
    /// <param name="validators">The discovered types.</param>
    internal void DiscoverValidators(IEnumerable<Type> validators) => _validators.UnionWith(validators);

    /// <summary>
    /// Contributes Chronicle's reader identity and types that must never be treated as collaborators.
    /// </summary>
    /// <param name="readerType">The command reader contract.</param>
    /// <param name="forbidden">Raw reader, storage and read-model types.</param>
    internal void RecognizeReaders(Type readerType, IEnumerable<Type> forbidden)
    {
        _readerType = readerType;
        _forbidden.UnionWith(forbidden);
    }

    /// <summary>
    /// Freezes only evidence belonging to the final descriptors this root actually builds.
    /// </summary>
    /// <param name="registrations">The final registration snapshot.</param>
    /// <returns>The independent root manifest.</returns>
    internal ProtectedDecisionRegistrationManifest Snapshot(IReadOnlyList<ServiceDescriptor> registrations) => new(
        registrations,
        _origins.Where(pair => registrations.Contains(pair.Key)).ToDictionary(pair => pair.Key, pair => pair.Value, (IEqualityComparer<ServiceDescriptor>)ReferenceEqualityComparer.Instance),
        _validators.ToHashSet(),
        _forbidden.ToHashSet(),
        _readerType);
}
