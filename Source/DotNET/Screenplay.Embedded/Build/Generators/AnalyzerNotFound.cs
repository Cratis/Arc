// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Embedded.Build.Generators;

/// <summary>
/// Exception that gets thrown when an analyzer the project compiles with is not on disk.
/// </summary>
/// <param name="path">The path the build stated.</param>
/// <remarks>
/// Skipping it would run the generators of every other analyzer and produce a compilation missing whatever this
/// one would have added, which is a silently wrong description of the application.
/// </remarks>
public class AnalyzerNotFound(string path)
    : Exception($"The analyzer '{path}' the project compiles with does not exist.");
