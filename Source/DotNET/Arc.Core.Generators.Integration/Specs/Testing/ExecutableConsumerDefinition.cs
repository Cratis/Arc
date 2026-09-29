// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Core.Generators.Integration.Specs.Testing;

/// <summary>
/// Describes an executable package consumer with a project reference to a library that consumes the same package.
/// </summary>
/// <param name="Name">The scenario name.</param>
/// <param name="PackageId">The package the executable and the library reference.</param>
/// <param name="DisableTransitiveProjectReferences">Whether the executable sets <c>DisableTransitiveProjectReferences</c>.</param>
/// <param name="ExpectsProjectReferenceRegistration">Whether the executable is expected to get a generated project reference registration.</param>
public sealed record ExecutableConsumerDefinition(
    string Name,
    string PackageId,
    bool DisableTransitiveProjectReferences,
    bool ExpectsProjectReferenceRegistration);
