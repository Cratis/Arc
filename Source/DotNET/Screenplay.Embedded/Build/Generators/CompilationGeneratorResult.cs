// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Embedded.Build.Generators;

/// <summary>
/// Represents the outcome of running the source generators of a project over a compilation.
/// </summary>
/// <param name="Compilation">The compilation holding the generated source alongside the authored source.</param>
/// <param name="Diagnostics">Everything the generators reported, with nothing of theirs left out.</param>
/// <remarks>
/// A generator crashing is not carried here - it is thrown, because a compilation missing the source a generator
/// was supposed to add describes an application that does not exist. What is carried is what the generators chose
/// to say about an application they did understand, which is for the build to report at the severity it was given.
/// </remarks>
public record CompilationGeneratorResult(Compilation Compilation, IReadOnlyList<Diagnostic> Diagnostics);
