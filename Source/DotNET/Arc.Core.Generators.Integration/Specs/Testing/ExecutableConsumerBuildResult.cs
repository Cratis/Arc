// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Core.Generators.Integration.Specs.Testing;

/// <summary>
/// Represents one executable consumer restore and build result.
/// </summary>
/// <param name="Definition">The executable consumer definition.</param>
/// <param name="BuildWasClean">Whether the build completed with zero warnings and zero errors.</param>
/// <param name="GeneratedProjectReferenceRegistrations">The generated project reference registration sources.</param>
public sealed record ExecutableConsumerBuildResult(
    ExecutableConsumerDefinition Definition,
    bool BuildWasClean,
    IReadOnlyCollection<string> GeneratedProjectReferenceRegistrations);
