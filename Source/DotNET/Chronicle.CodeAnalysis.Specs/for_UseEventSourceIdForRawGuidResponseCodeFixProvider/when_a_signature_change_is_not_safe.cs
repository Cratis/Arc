// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using static Cratis.Arc.Chronicle.CodeAnalysis.for_CommandEventSourceIdAnalyzer.when_validating_raw_guid_response.given.response_source;
using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.CodeFixVerifier<Cratis.Arc.Chronicle.CodeAnalysis.CommandEventSourceIdAnalyzer, Cratis.Arc.Chronicle.CodeAnalysis.CodeFixes.UseEventSourceIdForRawGuidResponseCodeFixProvider>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_UseEventSourceIdForRawGuidResponseCodeFixProvider;

public class when_a_signature_change_is_not_safe
{
    [Fact] public void should_not_offer_an_unverified_combination_of_signature_changes() => new CodeFixes.UseEventSourceIdForRawGuidResponseCodeFixProvider().GetFixAllProvider().ShouldBeNull();

    [Theory]
    [InlineData("Task<(Guid, E)>", "=> Task.FromResult((Guid.NewGuid(), new E()));")]
    [InlineData("async Task<Result<(Guid, E), Failure>>", "{ await Task.Yield(); return (Guid.NewGuid(), new E()); }")]
    [InlineData("async Task<OneOf<(Guid, E), Failure>>", "{ await Task.Yield(); return (Guid.NewGuid(), new E()); }")]
    [InlineData("ValueTask<(Guid, E)>", "=> new ValueTask<(Guid, E)>((Guid.NewGuid(), new E()));")]
    [InlineData("Result<(Guid, E), Failure>", "=> Result<(Guid, E), Failure>.Success((Guid.NewGuid(), new E()));")]
    [InlineData("OneOf<(Guid, E), Failure>", "=> OneOf<(Guid, E), Failure>.FromT0((Guid.NewGuid(), new E()));")]
    [InlineData("Result<(Guid, E), Failure>", "=> (Guid.NewGuid(), new E());")]
    [InlineData("OneOf<(Guid, E), Failure>", "=> (Guid.NewGuid(), new E());")]
    [InlineData("(Guid, E)", "=> (Guid.NewGuid(), new E()); public Task<(Guid, E)> Consumer() => Task.FromResult(Handle());")]
    [InlineData("virtual (Guid, E)", "=> (Guid.NewGuid(), new E());")]
    public async Task should_withhold_the_fix_for_invariant_or_shared_contracts(string signature, string body) =>
        await VerifyCS.VerifyNoCodeFixAsync(Command(signature, body), Warning());

    [Fact]
    public async Task should_report_the_local_alias_use_without_rewriting_the_alias_or_keyed_command() =>
        await VerifyCS.VerifyNoCodeFixAsync(
            "using Response = (System.Guid Id, E Event);\n" + Command("Response", "=> (Guid.NewGuid(), new E());") +
            "\n[Command] public record Keyed([Key] Guid Id) { public Response Handle() => (Guid.NewGuid(), new E()); }",
            Warning());

    [Fact]
    public async Task should_report_the_derived_command_without_rewriting_a_shared_base_handler() =>
        await VerifyCS.VerifyNoCodeFixAsync(
            Preamble + @"
            public record Base { public (Guid, E) Handle() => (Guid.NewGuid(), new E()); }
            [Command] public record {|#0:C|} : Base;
            [Command] public record Keyed([Key] Guid Id) : Base;
            ",
            Warning());

    [Fact]
    public async Task should_withhold_a_change_that_breaks_an_implicit_interface_contract() =>
        await VerifyCS.VerifyNoCodeFixAsync(
            Preamble + @"
            public interface IHandler { (Guid, E) Handle(); }
            [Command] public record C : IHandler { public {|#0:(Guid, E)|} Handle() => (Guid.NewGuid(), new E()); }
            ",
            Warning());
}
