// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable MA0136 // The fixture contains source snippets, independent of platform line endings.

using Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Chronicle.CodeAnalysis.Specs.for_CommandAttributeAnalyzer;

public class when_a_lookup_names_a_missing_property
{
    [Fact]
    public async Task should_resolve_the_declaration() => await AnalyzerVerifier<CommandAttributeAnalyzer>.VerifyAnalyzerAsync(
        """
        using Cratis.Arc.Commands;
        using Cratis.Arc.Chronicle.ReadModels;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Projections.ModelBound;
        public class Command { public void Handle([LookUpBy("Missing")] Model model) { } } [FromEvent<Changed>] public class Model; [EventType("5908fb8b-80a7-45e8-8157-b6af86425c19")] public record Changed;
        """,
        AnalyzerVerifier<CommandAttributeAnalyzer>.Diagnostic("ARCCHR0031").WithSeverity(DiagnosticSeverity.Error));
}
