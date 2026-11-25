// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.ManifestFile;

/// <summary>
/// A namespace, which is a module, feature or slice, that carries a gate for everything in it and beneath it.
/// </summary>
/// <param name="Namespace">The namespace.</param>
/// <param name="Gate">The gate.</param>
public record ManifestScope(string Namespace, ManifestExpression Gate);
