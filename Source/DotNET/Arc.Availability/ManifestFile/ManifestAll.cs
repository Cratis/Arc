// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.ManifestFile;

/// <summary>
/// Holds when every item holds.
/// </summary>
/// <param name="Items">The items.</param>
public record ManifestAll(IReadOnlyList<ManifestExpression> Items) : ManifestExpression;
