// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.TypeDiscovery.Plugin;

/// <summary>
/// The one type this assembly exists to have discovered.
/// </summary>
/// <remarks>
/// It reaches a universe only through <see cref="PluginTypeDiscoveryProvider"/>, which only a module initializer
/// registers - so finding it means the universe was built after this assembly was initialized, and nothing else
/// about the arrangement can produce that result.
/// </remarks>
public class PluginMarker : IPluginMarker;
