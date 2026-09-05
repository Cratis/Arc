// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.EndToEnd;

/// <summary>
/// Represents what reading a project or a solution yielded.
/// </summary>
/// <param name="Compilations">The compilations to generate from, empty when nothing yielded one.</param>
/// <param name="Name">The name of the project that was asked for, or <see langword="null"/> when a solution was.</param>
/// <remarks>
/// An application read as several projects is named by none of them, so the generator offers no name and the caller
/// supplies one. Naming a single project is the caller already having said which one the application is, and losing
/// that on the way in would put the neutral default where the application's own name belongs.
/// </remarks>
public record LoadedApplication(IReadOnlyList<Compilation> Compilations, string? Name);
