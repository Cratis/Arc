// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.for_HostBuilderExtensions.when_adding_cratis_arc_core;

/// <summary>
/// A provider that registers <see cref="TheLateProvider"/> while it is being initialized.
/// </summary>
/// <remarks>
/// Providers are initialized from the <c>Types</c> constructor, after it has captured the set it is building
/// from - so a provider registered here lands in the registry too late for the universe being built and in time
/// for the next one. That produces the same observable ordering the assembly closure walk in
/// <c>AddBindingsByConvention</c> and <c>AddSelfBindings</c> produces for a universe built earlier in the same
/// <c>AddCratisArcCore</c> call: the provider set grows between the two reads. It gets there by a different
/// route - the public registry from inside a construction, rather than a module constructor outside one - which
/// is what makes it reachable from a specification at all.
/// </remarks>
public class TheProviderThatBringsInTheLateOne : ICanProvideAssembliesForDiscovery
{
    /// <inheritdoc/>
    public IEnumerable<Assembly> Assemblies => [];

    /// <inheritdoc/>
    public IEnumerable<Type> DefinedTypes => [];

    /// <inheritdoc/>
    public void Initialize() => GeneratedTypeDiscoveryRegistry.Register(new TheLateProvider());
}
