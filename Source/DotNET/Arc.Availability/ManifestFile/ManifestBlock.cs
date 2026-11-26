// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.ManifestFile;

/// <summary>
/// A named availability block in the manifest file.
/// </summary>
/// <param name="Name">The name of the block.</param>
/// <param name="Gate">The gate; <c>with</c> a block holds when this does.</param>
/// <param name="Description">What it is for; empty when not given.</param>
public record ManifestBlock(string Name, ManifestExpression Gate, string Description = "");
