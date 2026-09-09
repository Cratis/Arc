// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Cratis.Types;

namespace Cratis.Arc.TypeDiscovery.Plugin;

/// <summary>
/// Registers <see cref="PluginTypeDiscoveryProvider"/> from a module initializer, which is the only route by which
/// it ever reaches the registry.
/// </summary>
/// <remarks>
/// This is the shape the Fundamentals type discovery generator emits, and the reason a generated provider can
/// arrive after a universe has already been built: the runtime defers a module initializer until something in the
/// module is first accessed, and for an assembly reachable only through the reference closure that is the closure
/// walk in <c>AddBindingsByConvention</c> and <c>AddSelfBindings</c>.
/// </remarks>
internal static class PluginTypeDiscoveryRegistration
{
    /// <summary>
    /// Registers the provider with the process-wide registry.
    /// </summary>
    [ModuleInitializer]
    [SuppressMessage("Usage", "CA2255:The 'ModuleInitializer' attribute should not be used in libraries", Justification = "Reproducing what the Fundamentals type discovery generator emits is the entire purpose of this assembly.")]
    internal static void Register() => GeneratedTypeDiscoveryRegistry.Register(new PluginTypeDiscoveryProvider());
}
