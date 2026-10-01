// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Embedded.Build.Generators;

/// <summary>
/// Exception that gets thrown when the analyzer configuration files cannot be read as one configuration.
/// </summary>
/// <param name="errors">What Roslyn reported about the configuration.</param>
/// <remarks>
/// Two global configuration files setting the same key, or a file that is not valid configuration at all, leave
/// the generators reading values the compiler will not read. Guessing which one wins is not this build's call.
/// </remarks>
public class AnalyzerConfigurationIsInvalid(IEnumerable<Diagnostic> errors)
    : Exception($"The analyzer configuration of the project could not be read:{Environment.NewLine}{string.Join(Environment.NewLine, errors.Select(_ => _.GetMessage()))}");
