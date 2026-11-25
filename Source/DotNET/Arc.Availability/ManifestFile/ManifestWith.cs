// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.ManifestFile;

/// <summary>
/// Holds when the named availability block holds.
/// </summary>
/// <param name="Block">The name of a block in the manifest.</param>
public record ManifestWith(string Block) : ManifestExpression;
