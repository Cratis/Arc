// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.ManifestFile;

/// <summary>
/// Holds when the condition holds.
/// </summary>
/// <param name="Condition">The condition.</param>
public record ManifestWhen(ManifestCondition Condition) : ManifestExpression;
