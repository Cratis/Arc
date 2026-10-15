// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable MA0136 // Independent source fragments form one compilation unit.

using Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Chronicle.CodeAnalysis.Specs.for_PerStreamReadModelAnalyzer;

public class when_checking_stream_bindings
{
    const string Definitions = """
        using System.Threading.Tasks;
        using Cratis.Arc.Commands;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Arc.Chronicle.Commands;
        using Cratis.Arc.Chronicle.Streams;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.EventSources;
        using Cratis.Chronicle.Projections.ModelBound;
        using Cratis.Chronicle.ReadModels;
        [EventSource("BankAccount"), EventStream("Transactions"), EventStream("Limits")]
        public class Bank : IEventSource;
        [EventSource("Other"), EventStream("Transactions")]
        public class OtherBank : IEventSource;
        public record AccountId(string Value) : EventSourceId(Value);
        [EventType] public record Withdrawn(decimal Amount);
        [EventStream<Bank>("Transactions")]
        public record Month(AccountId Id, [AddFrom<Withdrawn>(nameof(Withdrawn.Amount))] decimal Total);
        public record Total(AccountId Id, [AddFrom<Withdrawn>(nameof(Withdrawn.Amount))] decimal Amount);
        """;
    const string Route = "[EventSource<Bank>(\"Transactions\"), EventStreamId(\"{Period}\")]";

    [Theory]
    [InlineData("Off")]
    [InlineData("Measure")]
    [InlineData("On")]
    public Task should_accept_a_matching_route_in_every_mode(string mode) => Verify(Definitions + Route +
        $"[Command, ReadProtection(ReadProtectionMode.{mode})] public record Withdraw(AccountId Account, string Period) {{ public Withdrawn Handle(Month month) => new(1); }}");

    [Theory]
    [InlineData("")]
    [InlineData("[EventSource<Bank>]")]
    [InlineData("[EventSource<Bank>(\"Limits\"), EventStreamId(\"{Period}\")]")]
    [InlineData("[EventSource<OtherBank>(\"Transactions\"), EventStreamId(\"{Period}\")]")]
    [InlineData("[EventSource<Bank>(\"Transactions\")]")]
    [InlineData("[EventSource<Bank>(\"Transactions\"), EventStreamId(\"Default\")]")]
    public Task should_refuse_missing_or_different_routes(string route) => Verify(Definitions + route +
        "[Command] public record Withdraw(AccountId Account, string Period) { public Withdrawn Handle(Month month) => new(1); }",
        Error());

    [Fact]
    public Task should_check_validator_constructor_parameters() => Verify(Definitions +
        "[Command] public record Withdraw(AccountId Account) { public Withdrawn Handle() => new(1); } public class Validator(Month month) : CommandValidator<Withdraw>;",
        Error());

    [Fact]
    public Task should_accept_matching_string_routes() => Verify(Definitions +
        "[Command, EventSourceType(\"BankAccount\"), EventStreamType(\"Transactions\"), EventStreamId(\"{Period}\")] public record Withdraw(AccountId Account, string Period) { public Withdrawn Handle(Month month) => new(1); }");

    [Fact]
    public Task should_accept_a_stream_id_provider() => Verify(Definitions +
        "[Command, EventSource<Bank>(\"Transactions\")] public record Withdraw(AccountId Account) : ICanProvideEventStreamId { public EventStreamId GetEventStreamId() => \"month\"; public Withdrawn Handle(Month month) => new(1); }");

    [Fact]
    public Task should_refuse_lookup_even_when_the_property_exists() => Verify(Definitions + Route +
        "[Command] public record Withdraw(AccountId Account, string Period) { public Withdrawn Handle([LookUpBy(nameof(Account))] Month month) => new(1); }",
        Error().WithArguments("LookUpBy"));

    [Fact]
    public Task should_not_infer_a_provided_parameter_route() => Verify(Definitions +
        "[Command] public record Withdraw(AccountId Account) { public Month Provide() => new(Account, 0); public Withdrawn Handle(Month month) => new(1); }");

    [Theory]
    [InlineData("Measure")]
    [InlineData("On")]
    public Task should_report_a_whole_source_read(string mode) => Verify(Definitions + Route +
        $"[Command, ReadProtection(ReadProtectionMode.{mode})] public record Withdraw(AccountId Account, string Period) {{ public Withdrawn Handle(Month month, Total total) => new(1); }}",
        Info());

    [Fact]
    public Task should_report_validator_and_handler_combination() => Verify(Definitions + Route +
        "[Command, ReadProtection(ReadProtectionMode.On)] public record Withdraw(AccountId Account, string Period) { public Withdrawn Handle(Total total) => new(1); } public class Validator(Month month) : CommandValidator<Withdraw>;",
        Info());

    [Fact]
    public Task should_not_report_advisory_or_off_whole_source_reads() => Verify(Definitions + Route +
        "[Command, ReadProtection(ReadProtectionMode.On)] public record Withdraw(AccountId Account, string Period) { public Withdrawn Handle(Month month, [Cratis.Arc.Chronicle.ReadModels.Unprotected(\"context\")] Total total) => new(1); }");

    [Fact]
    public Task should_not_report_two_models_of_the_same_stream() => Verify(Definitions + Route +
        "[Command, ReadProtection(ReadProtectionMode.On)] public record Withdraw(AccountId Account, string Period) { public Withdrawn Handle(Month month, Month same) => new(1); }");

    [Theory]
    [InlineData("ConcurrencyDimensions.None", "ConcurrencyDimensions.None", false)]
    [InlineData("ConcurrencyDimensions.EventSourceId", "ConcurrencyDimensions.None", true)]
    [InlineData("ConcurrencyDimensions.EventSourceId", "ConcurrencyDimensions.EventSourceId | ConcurrencyDimensions.EventStreamType | ConcurrencyDimensions.EventStreamId", false)]
    [InlineData("ConcurrencyDimensions.None", "ConcurrencyDimensions.EventSourceId | ConcurrencyDimensions.EventStreamType", true)]
    [InlineData("ConcurrencyDimensions.None", "ConcurrencyDimensions.EventStreamId", false)]
    public Task should_check_effective_declared_concurrency(string source, string stream, bool widens) => Verify(
        Definitions.Replace("EventSource(\"BankAccount\"), EventStream(\"Transactions\")", $"EventSource(\"BankAccount\", Concurrency = {source}), EventStream(\"Transactions\", Concurrency = {stream})", StringComparison.Ordinal) + Route +
        "[Command, ReadProtection(ReadProtectionMode.On)] public record Withdraw(AccountId Account, string Period) { public Withdrawn Handle(Month month) => new(1); }",
        widens ? [Info()] : []);

    [Fact]
    public Task should_warn_about_validator_first_stream_decisions() => AnalyzerVerifier<ReadProtectionParameterAnalyzer>.VerifyAnalyzerAsync(Definitions + Route +
        "[Command, ReadProtection(ReadProtectionMode.On)] public record Withdraw(AccountId Account, string Period) { public Task<StreamDecision> Provide(IStreamReads reads) => reads.ForCommand(); public Cratis.Chronicle.EventSequences.EventsWithConcurrencyScopes Handle(StreamDecision decision) => decision.Append(new Withdrawn(1)); } public class Validator(Month month) : CommandValidator<Withdraw>;",
        AnalyzerVerifier<ReadProtectionParameterAnalyzer>.Diagnostic("ARCCHR0036").WithSeverity(DiagnosticSeverity.Warning));

    static DiagnosticBuilder Error() => AnalyzerVerifier<PerStreamReadModelAnalyzer>.Diagnostic("ARCCHR0037").WithSeverity(DiagnosticSeverity.Error);
    static DiagnosticBuilder Info() => AnalyzerVerifier<PerStreamReadModelAnalyzer>.Diagnostic("ARCCHR0038").WithSeverity(DiagnosticSeverity.Info);
    static Task Verify(string source, params ExpectedDiagnostic[] diagnostics) => AnalyzerVerifier<PerStreamReadModelAnalyzer>.VerifyAnalyzerAsync(source, diagnostics);
}
