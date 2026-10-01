// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Embedded.Build.Generators;

/// <summary>
/// Exception that gets thrown when a source generator crashed while running over the compilation.
/// </summary>
/// <param name="crashes">The diagnostics the driver reported for the generators that crashed.</param>
/// <remarks>
/// The driver answers with a compilation either way, one that simply lacks whatever the crashed generator would
/// have added. Continuing with it would describe an application with a hole in it that nothing downstream can see.
/// </remarks>
public class GeneratorFailed(IEnumerable<Diagnostic> crashes)
    : Exception($"A source generator failed while generating source:{Environment.NewLine}{string.Join(Environment.NewLine, crashes.Select(_ => _.GetMessage()))}")
{
    /// <summary>
    /// The identifier the compiler reports a generator that could not be initialized under.
    /// </summary>
    public const string InitializationCrash = "CS8784";

    /// <summary>
    /// The identifier the compiler reports a generator that threw while generating under.
    /// </summary>
    public const string GenerationCrash = "CS8785";

    /// <summary>
    /// Determines whether a diagnostic says a generator crashed.
    /// </summary>
    /// <param name="diagnostic">The diagnostic to check.</param>
    /// <returns>True when the diagnostic reports a crash, false otherwise.</returns>
    public static bool IsCrash(Diagnostic diagnostic) =>
        diagnostic.Id == InitializationCrash || diagnostic.Id == GenerationCrash;
}
