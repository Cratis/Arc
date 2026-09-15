// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing;
using static Cratis.Arc.Chronicle.CodeAnalysis.for_CommandEventSourceIdAnalyzer.when_validating_raw_guid_response.given.response_source;
using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.CodeFixVerifier<Cratis.Arc.Chronicle.CodeAnalysis.CommandEventSourceIdAnalyzer, Cratis.Arc.Chronicle.CodeAnalysis.CodeFixes.UseEventSourceIdForRawGuidResponseCodeFixProvider>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_UseEventSourceIdForRawGuidResponseCodeFixProvider;

public class when_rewriting_supported_local_tuples
{
    [Theory]
    [InlineData("(Guid Id, E Event)", "=> (Guid.NewGuid(), new E());")]
    [InlineData("(E Event, Guid Id)", "=> (new E(), Guid.NewGuid());")]
    [InlineData("(Identity Id, E Event)", "=> (Guid.NewGuid(), new E());")]
    [InlineData("async Task<(Guid Id, E Event)>", "{ await Task.Yield(); return (Guid.NewGuid(), new E()); }")]
    [InlineData("async ValueTask<(Guid Id, E Event)>", "{ await Task.Yield(); return (Guid.NewGuid(), new E()); }")]
    [InlineData("(Guid Id, E[] Events)", "=> (Guid.NewGuid(), new[] { new E() });")]
    [InlineData("(Guid Id, IEnumerable<E> Events)", "=> (Guid.NewGuid(), new[] { new E() });")]
    [InlineData("(Guid Id, OneOf<E, Failure> Event)", "=> (Guid.NewGuid(), new E());")]
    [InlineData("(Guid Id, E Event, EventForEventSourceId Targeted)", "=> (Guid.NewGuid(), new E(), new EventForEventSourceId(EventSourceId.New(), new E()));")]
    public async Task should_preserve_names_and_compile_the_local_identity_change(string signature, string body)
    {
        var source = "using Identity = System.Guid;\n" + Command(signature, body);
        var fixedSignature = signature.Replace("Guid Id", "global::Cratis.Chronicle.Events.EventSourceId<global::System.Guid> Id", StringComparison.Ordinal)
            .Replace("Identity Id", "global::Cratis.Chronicle.Events.EventSourceId<global::System.Guid> Id", StringComparison.Ordinal);
        var fixedSource = SourceMarker.Parse(source).Source.Replace(signature, fixedSignature, StringComparison.Ordinal);
        await VerifyCS.VerifyCodeFixAsync(source, fixedSource, Warning());
    }

    [Fact]
    public async Task should_leave_an_unrelated_command_warning_in_place()
    {
        var source = Command("(Guid, E)", "=> (Guid.NewGuid(), new E());") + "\n[Command] public record Other { public {|#1:(Guid, E)|} Handle() => (Guid.NewGuid(), new E()); }";
        const string fixedSource = Preamble + "\n[Command] public record C { public (global::Cratis.Chronicle.Events.EventSourceId<global::System.Guid>, E) Handle() => (Guid.NewGuid(), new E()); }\n[Command] public record Other { public (Guid, E) Handle() => (Guid.NewGuid(), new E()); }";
        await VerifyCS.VerifyCodeFixAsync(source, fixedSource, Warning(), Warning("Other"));
    }
}
