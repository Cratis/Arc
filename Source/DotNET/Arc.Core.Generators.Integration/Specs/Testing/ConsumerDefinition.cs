// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Core.Generators.Integration.Specs.Testing;

/// <summary>
/// Describes expected analyzer assets for one package consumer.
/// </summary>
/// <param name="Name">The scenario name.</param>
/// <param name="PackageIds">The referenced package identifiers.</param>
/// <param name="ArcAnalyzerCount">The expected ARC analyzer count.</param>
/// <param name="ArcCodeFixCount">The expected ARC code-fix count.</param>
/// <param name="ArcGeneratorCount">The expected Arc generator count.</param>
/// <param name="ChronicleAnalyzerCount">The expected ARCCHR analyzer count.</param>
/// <param name="ChronicleCodeFixCount">The expected ARCCHR code-fix count.</param>
public sealed record ConsumerDefinition(
    string Name,
    IReadOnlyCollection<string> PackageIds,
    int ArcAnalyzerCount,
    int ArcCodeFixCount,
    int ArcGeneratorCount,
    int ChronicleAnalyzerCount,
    int ChronicleCodeFixCount);
