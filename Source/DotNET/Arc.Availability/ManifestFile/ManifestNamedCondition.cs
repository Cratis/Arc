// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.ManifestFile;

/// <summary>
/// A condition that is code in the engine that wrote the manifest, so it is named and described but cannot be evaluated from the file.
/// </summary>
/// <param name="Name">The name of the condition, for example the full name of its type.</param>
/// <param name="Description">The condition as authored.</param>
public record ManifestNamedCondition(string Name, string Description) : ManifestCondition;
