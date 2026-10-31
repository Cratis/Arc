// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable MA0136 // Compiler source fixtures are independent of platform line endings.
#pragma warning disable SA1117 // Snippet arguments and expected diagnostics are displayed separately.

using Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing;
using Cratis.Arc.Commands;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Chronicle.CodeAnalysis.Specs.for_CommandDecisionReadAnalyzer;

public class when_a_handler_takes_a_decision_reader_or_token : Specification
{
    [Theory]
    [InlineData(ReadProtectionMode.Off)]
    [InlineData(ReadProtectionMode.Measure)]
    [InlineData(ReadProtectionMode.On)]
    public Task should_advise_on_a_synchronous_reader_parameter_in_every_mode(ReadProtectionMode mode) => Verify($$"""
        [Command, ReadProtection(ReadProtectionMode.{{mode}})]
        public record Decide
        {
            public void Handle({|#0:IDecisionReads reads|}) { }
        }
        """, Advice("IDecisionReads"));

    [Theory]
    [InlineData(ReadProtectionMode.Off)]
    [InlineData(ReadProtectionMode.Measure)]
    [InlineData(ReadProtectionMode.On)]
    public Task should_advise_on_an_async_token_parameter_in_every_mode(ReadProtectionMode mode) => Verify($$"""
        [Command, ReadProtection(ReadProtectionMode.{{mode}})]
        public record Decide
        {
            public async Task Handle({|#0:DecisionRead<State> read|}) { await Task.CompletedTask; }
        }
        """, Advice("DecisionRead"));

    [Theory]
    [InlineData(ReaderDeclaration.ReaderAlias)]
    [InlineData(ReaderDeclaration.TokenAlias)]
    [InlineData(ReaderDeclaration.DerivedInterface)]
    public Task should_resolve_aliases_and_derived_reader_interfaces(ReaderDeclaration kind)
    {
        var (declaration, type, expectedType) = kind switch
        {
            ReaderDeclaration.ReaderAlias => ("using Reader = Cratis.Chronicle.ReadModels.IDecisionReads;", "Reader", "IDecisionReads"),
            ReaderDeclaration.TokenAlias => ("using Token = Cratis.Chronicle.ReadModels.DecisionRead<State>;", "Token", "DecisionRead"),
            _ => ("public interface Reader : IDecisionReads;", "Reader", "Reader")
        };
        return Verify($$"""
            {{declaration}}
            [Command] public record Decide
            {
                public void Handle({|#0:{{type}} read|}) { }
            }
            """, Advice(expectedType));
    }

    [Fact]
    public Task should_report_each_offending_parameter() => Verify("""
        [Command] public record Decide
        {
            public void Handle({|#0:IDecisionReads reads|}, {|#1:DecisionRead<State> read|}) { }
        }
        """, Advice("IDecisionReads"), Advice("DecisionRead"));

    [Fact]
    public Task should_not_report_provide_acquiring_plain_state() => Verify("""
        [Command, ReadProtection(ReadProtectionMode.On)] public record Decide
        {
            public async Task<State> Provide(IDecisionReads reads) =>
                (await reads.Get<State>(new ReadModelKey("computed"))).Instance;
            public void Handle(State state) { }
        }
        """);

    [Fact]
    public Task should_not_report_a_validator_constructor() => Verify("""
        [Command] public record Decide { public void Handle() { } }
        public class Validator(IDecisionReads reads) : CommandValidator<Decide>;
        """);

    [Fact]
    public Task should_not_report_non_commands_or_non_handler_methods() => Verify("""
        public class Service { public void Handle(IDecisionReads reads) { } }
        [Command] public class Decide
        {
            public Decide(IDecisionReads reads) { }
            public void Handle() { }
            public void Helper(DecisionRead<State> read) { }
            public static void Handle(IDecisionReads reads) { }
            private void Handle(DecisionRead<State> read) { }
        }
        """);

    [Fact]
    public Task should_not_match_unrelated_types_by_short_name() => Verify("""
        public interface IDecisionReads;
        public record DecisionRead<T>;
        [Command] public record Decide
        {
            public void Handle(IDecisionReads reads, DecisionRead<State> read) { }
        }
        """);

    [Theory]
    [InlineData(PlainParameter.Lookup)]
    [InlineData(PlainParameter.Advisory)]
    [InlineData(PlainParameter.Unannotated)]
    public Task should_not_report_plain_model_parameters(PlainParameter kind)
    {
        var parameter = kind switch
        {
            PlainParameter.Lookup => "[LookUpBy(nameof(Product))] State state",
            PlainParameter.Advisory => "[Unprotected(\"display context\")] State state",
            _ => "State state"
        };
        return Verify($$"""
            [Command, ReadProtection(ReadProtectionMode.On)] public record Decide(string Product)
            {
                public void Handle({{parameter}}) { }
            }
            """);
    }

    [Fact]
    public Task should_not_report_a_plain_per_stream_model() => Verify("""
        [Cratis.Chronicle.EventSources.EventSource("BankAccount")]
        [Cratis.Chronicle.EventSources.EventStream("Transactions")]
        public class BankAccount : Cratis.Chronicle.EventSources.IEventSource;
        [Cratis.Arc.Queries.ModelBound.ReadModel, Cratis.Chronicle.ReadModels.EventStream<BankAccount>("Transactions")]
        public record MonthState;
        [Command, ReadProtection(ReadProtectionMode.On)] public record Decide
        {
            public void Handle(MonthState state) { }
        }
        """);

    [Fact]
    public Task should_resolve_fully_qualified_parameter_types() => Verify("""
        [Command] public record Decide
        {
            public void Handle({|#0:global::Cratis.Chronicle.ReadModels.IDecisionReads reads|}) { }
        }
        """, Advice("IDecisionReads"));

    [Fact]
    public Task should_not_analyze_generated_code() => AnalyzerVerifier<CommandDecisionReadAnalyzer>.VerifyAnalyzerAsync("""
        // <auto-generated/>
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.ReadModels;
        [Command] public record Decide { public void Handle(IDecisionReads reads) { } }
        """);

    public enum ReaderDeclaration
    {
        ReaderAlias = 0,
        TokenAlias = 1,
        DerivedInterface = 2
    }

    public enum PlainParameter
    {
        Lookup = 0,
        Advisory = 1,
        Unannotated = 2
    }

    static ExpectedDiagnostic Advice(string type) => AnalyzerVerifier<CommandDecisionReadAnalyzer>.Diagnostic("ARCCHR0039")
        .WithSeverity(DiagnosticSeverity.Info).WithArguments("Decide", type, "plain parameter", "LookUpBy", "per-stream", "Provide(IDecisionReads)");

    static Task Verify(string declaration, params ExpectedDiagnostic[] expected) =>
        AnalyzerVerifier<CommandDecisionReadAnalyzer>.VerifyAnalyzerAsync($$"""
            using System.Threading.Tasks;
            using Cratis.Arc.Chronicle.ReadModels;
            using Cratis.Arc.Commands;
            using Cratis.Arc.Commands.ModelBound;
            using Cratis.Chronicle.ReadModels;
            {{declaration}}
            public record State;
            """, expected);
}
