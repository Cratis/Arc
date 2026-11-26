// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Cratis.Arc.Availability.ManifestFile;

/// <summary>
/// A gate in the manifest file, as an expression tree any engine can evaluate.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(ManifestFor), "for")]
[JsonDerivedType(typeof(ManifestNot), "not")]
[JsonDerivedType(typeof(ManifestWhen), "when")]
[JsonDerivedType(typeof(ManifestWith), "with")]
[JsonDerivedType(typeof(ManifestAll), "all")]
[JsonDerivedType(typeof(ManifestAny), "any")]
public abstract record ManifestExpression;
