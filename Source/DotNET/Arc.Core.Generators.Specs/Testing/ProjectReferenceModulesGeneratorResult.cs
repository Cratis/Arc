// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Generators.Specs.Testing;

/// <summary>
/// Represents the result of running the <see cref="ProjectReferenceModulesGenerator"/>.
/// </summary>
/// <param name="Source">The generated source, or null when nothing was generated.</param>
/// <param name="Errors">The generator diagnostics and the compilation errors after generation.</param>
/// <param name="Compilation">The compilation including the generated source.</param>
public sealed record ProjectReferenceModulesGeneratorResult(string? Source, IReadOnlyList<Diagnostic> Errors, Compilation Compilation);
