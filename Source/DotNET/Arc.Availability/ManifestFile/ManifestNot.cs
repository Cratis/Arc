// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.ManifestFile;

/// <summary>
/// Holds when the audience is not held.
/// </summary>
/// <param name="Audience">The audience.</param>
public record ManifestNot(string Audience) : ManifestExpression;
