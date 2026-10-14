// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable MA0136 // The fixture contains source snippets, independent of platform line endings.

using Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Chronicle.CodeAnalysis.Specs.for_CommandDecisionReadAnalyzer;

public class when_a_nested_command_is_inside_an_unprotected_type
{
    [Fact]
    public async Task should_still_report_an_unprotected_read() => await AnalyzerVerifier<CommandDecisionReadAnalyzer>.VerifyAnalyzerAsync(
        """
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Arc.Chronicle.ReadModels;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Projections.ModelBound;
        [EventType("5908fb8b-80a7-45e8-8157-b6af86425c19")] public record Changed;
        [FromEvent<Changed>] public record Model;
        [Unprotected] public class Outer
        {
            [Command] public class Command { public Changed Handle(Model model) => new(); }
        }
        """,
        AnalyzerVerifier<CommandDecisionReadAnalyzer>.Diagnostic("ARCCHR0011").WithSeverity(DiagnosticSeverity.Info));
}
