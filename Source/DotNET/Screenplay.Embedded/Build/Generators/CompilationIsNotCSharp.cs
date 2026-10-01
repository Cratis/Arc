// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Embedded.Build.Generators;

/// <summary>
/// Exception that gets thrown when generators are asked to run over a compilation that is not a C# one.
/// </summary>
/// <param name="compilation">The compilation that was handed in.</param>
/// <remarks>
/// The driver, the parse options and the generators are all the C# ones. Anything else is a caller mistake rather
/// than a project that happens to have no generators.
/// </remarks>
public class CompilationIsNotCSharp(Compilation compilation)
    : Exception($"Source generators can only be run over a C# compilation, and '{compilation.AssemblyName}' is a '{compilation.Language}' one.");
