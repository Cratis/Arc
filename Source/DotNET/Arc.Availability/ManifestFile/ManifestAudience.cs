// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.ManifestFile;

/// <summary>
/// An audience in the manifest file.
/// </summary>
/// <param name="Name">The name of the audience.</param>
/// <param name="Description">What it is for; empty when not given.</param>
public record ManifestAudience(string Name, string Description = "");
