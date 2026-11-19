// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable MA0136 // Compiler source fixtures are independent of platform line endings.

using Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Chronicle.CodeAnalysis.Specs.for_CommandDecisionReadAnalyzer;

public class when_classifying_event_return_shapes : Specification
{
    [Theory]
    [InlineData("Changed")]
    [InlineData("Changed[]")]
    [InlineData("object[]")]
    [InlineData("Changed[][]")]
    [InlineData("IEnumerable<Changed>")]
    [InlineData("IEnumerable<object>")]
    [InlineData("IReadOnlyList<Changed>")]
    [InlineData("Cratis.Chronicle.EventSequences.EventForEventSourceId")]
    [InlineData("Cratis.Chronicle.EventSequences.EventsWithConcurrencyScopes")]
    [InlineData("(int, Changed)")]
    [InlineData("Task<(int, Changed)>")]
    [InlineData("ValueTask<Changed>")]
    [InlineData("OneOf.OneOf<int, Changed>")]
    [InlineData("Cratis.Monads.Result<int, Changed>")]
    [InlineData("Choice<Changed, string>")]
    public Task should_report_the_plain_model_for_event_shapes(string returnType) =>
        AnalyzerVerifier<CommandDecisionReadAnalyzer>.VerifyAnalyzerAsync(
            Source(returnType),
            AnalyzerVerifier<CommandDecisionReadAnalyzer>.Diagnostic("ARCCHR0011")
                .WithSeverity(DiagnosticSeverity.Info)
                .WithArguments("Change", "State", "Enable ReadProtection(On)"));

    [Theory]
    [InlineData("int")]
    [InlineData("object")]
    [InlineData("int[]")]
    [InlineData("int[][]")]
    [InlineData("IEnumerable<int>")]
    [InlineData("Box<Changed>")]
    [InlineData("int?")]
    [InlineData("(int, string)")]
    [InlineData("Task<Cratis.Monads.Result<int, string>>")]
    [InlineData("Choice<int, string>")]
    public Task should_not_report_the_plain_model_for_non_event_shapes(string returnType) =>
        AnalyzerVerifier<CommandDecisionReadAnalyzer>.VerifyAnalyzerAsync(Source(returnType));

    static string Source(string returnType) => $$"""
        {{DecisionAnalyzerSource.Definitions}}
        public record Box<T>;
        public class Choice<TEvent, TError> : OneOf.OneOfBase<TEvent, TError>
        {
            public Choice() : base(default(OneOf.OneOf<TEvent, TError>)) { }
        }
        [Command]
        public record Change
        {
            public {{returnType}} Handle(State state) => default!;
        }
        """;
}
