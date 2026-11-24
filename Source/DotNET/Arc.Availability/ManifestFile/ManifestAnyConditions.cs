// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.ManifestFile;

/// <summary>
/// Holds when any condition holds.
/// </summary>
/// <param name="Items">The conditions.</param>
public record ManifestAnyConditions(IReadOnlyList<ManifestCondition> Items) : ManifestCondition;
