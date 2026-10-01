// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Embedded.Build.Generators;

/// <summary>
/// Exception that gets thrown when an analyzer the project compiles with could not be read for its generators.
/// </summary>
/// <param name="failures">What was reported for every analyzer that could not be read.</param>
/// <remarks>
/// Roslyn reports a generator it cannot instantiate and carries on with the rest, which is how a build quietly
/// ends up compiling an application whose generated half never existed. Here it ends the run instead.
/// </remarks>
public class GeneratorsCouldNotBeLoaded(IEnumerable<string> failures)
    : Exception($"Source generators could not be loaded:{Environment.NewLine}{string.Join(Environment.NewLine, failures)}");
