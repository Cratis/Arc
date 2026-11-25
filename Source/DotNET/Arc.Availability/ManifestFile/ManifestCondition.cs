// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Cratis.Arc.Availability.ManifestFile;

/// <summary>
/// A condition in the manifest file.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(ManifestCompare), "compare")]
[JsonDerivedType(typeof(ManifestAllConditions), "all")]
[JsonDerivedType(typeof(ManifestAnyConditions), "any")]
[JsonDerivedType(typeof(ManifestNamedCondition), "named")]
public abstract record ManifestCondition;
