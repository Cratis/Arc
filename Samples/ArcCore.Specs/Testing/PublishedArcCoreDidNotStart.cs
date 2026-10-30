// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Samples.ArcCore.Specs.Testing;

/// <summary>
/// The exception that is thrown when the published ArcCore executable does not signal readiness.
/// </summary>
/// <param name="exitCode">The exit code, if the process exited.</param>
/// <param name="output">The captured process output.</param>
public class PublishedArcCoreDidNotStart(int? exitCode, string output) : Exception(
    $"Published ArcCore did not start. Exit code: {exitCode?.ToString() ?? "unavailable"}.{Environment.NewLine}{output}");
