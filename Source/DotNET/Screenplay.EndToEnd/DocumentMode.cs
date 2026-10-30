// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.EndToEnd;

/// <summary>
/// Describes a document to generate and check from a loaded application.
/// </summary>
/// <param name="Name">The name of the document mode.</param>
/// <param name="Output">The full path of the document to write.</param>
/// <param name="AuthoringOnlyConstructs">Whether to include authoring-only constructs.</param>
/// <param name="Expected">The expectations file, or <see langword="null"/> when none applies.</param>
public record DocumentMode(string Name, string Output, bool AuthoringOnlyConstructs, string? Expected);
