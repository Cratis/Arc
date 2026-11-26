// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability;

/// <summary>
/// Base type of a scope marker: gates everything in the namespace of the marker and in the namespaces beneath it.
/// </summary>
/// <remarks>
/// Put the marker in the namespace of a module, a feature or a slice and gate it with the availability attributes:
/// <c>[AvailableFor&lt;Plus&gt;] public class Feature : AvailabilityScope;</c>. Module, feature and slice names are read from
/// the namespace as the proxy generator reads them. The marker replaces <c>ArcAvailabilityOptions.GateNamespace</c>.
/// </remarks>
public abstract class AvailabilityScope;
