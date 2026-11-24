// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.ManifestFile;

/// <summary>
/// A named condition in the manifest file, one that is code and so is not evaluated from the file.
/// </summary>
/// <param name="Name">The name of the condition.</param>
/// <param name="Description">The condition as authored.</param>
public record ManifestNamedConditionEntry(string Name, string Description);
