// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable MA0136 // The fixture contains source snippets, independent of platform line endings.

using Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Arc.Chronicle.CodeAnalysis.Specs.for_CommandDecisionReadAnalyzer;

public class when_unprotected_is_inherited_from_a_base_command
{
    [Fact]
    public async Task should_not_report_an_unprotected_read() => await AnalyzerVerifier<CommandDecisionReadAnalyzer>.VerifyAnalyzerAsync("""
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Arc.Chronicle.ReadModels;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Projections.ModelBound;
        [EventType("5908fb8b-80a7-45e8-8157-b6af86425c19")] public record Changed;
        [FromEvent<Changed>] public record Model;
        [Unprotected] public class Base;
        [Command] public class Command : Base { public Changed Handle(Model model) => new(); }
        """);
}
