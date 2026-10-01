// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Embedded.Build.Generators;

/// <summary>
/// Exception that gets thrown when an analyzer configuration file the generators are configured by is not on disk.
/// </summary>
/// <param name="path">The path the build stated.</param>
/// <remarks>
/// The build properties a generator reads live in these files. Running without one means running generators that
/// are configured differently from the ones the C# compiler will run moments later.
/// </remarks>
public class AnalyzerConfigurationNotFound(string path)
    : Exception($"The analyzer configuration file '{path}' the generators are configured by does not exist.");
